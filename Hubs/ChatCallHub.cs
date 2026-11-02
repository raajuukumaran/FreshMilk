using FreshMilk.API.Models.Users;
using FreshMilk.API.Repositories.Interfaces;
using FreshMilk.API.Responses.Chat;
using FreshMilk.API.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using System.Security.Cryptography;
using System.Text;

namespace FreshMilk.API.Hubs;

public sealed class ChatCallHub : Hub
{
    private const int MaxSessionDescriptionLength = 100_000;
    private const int MaxIceCandidateLength = 4_096;
    private static readonly object StateLock = new();
    private static readonly Dictionary<string, CallSession> Calls = new();
    private static readonly Dictionary<long, string> ActiveCallByUser = new();
    private static readonly Dictionary<long, HashSet<string>> ConnectionsByUser = new();
    private static readonly Dictionary<long, DateTimeOffset> LastSeenByUser = new();

    private readonly ICallAccessTokenService _tokens;
    private readonly IChatService _chatService;
    private readonly IUsersRepository _users;
    private readonly IConfiguration _configuration;

    public ChatCallHub(
        ICallAccessTokenService tokens,
        IChatService chatService,
        IUsersRepository users,
        IConfiguration configuration)
    {
        _tokens = tokens;
        _chatService = chatService;
        _users = users;
        _configuration = configuration;
    }

    public IReadOnlyList<CallIceServer> GetIceServers()
    {
        var identity = GetIdentity();
        var stunUrls = _configuration.GetSection("CallSignaling:StunUrls")
            .Get<string[]>() ?? ["stun:stun.l.google.com:19302"];
        var servers = new List<CallIceServer>();
        if (stunUrls.Length > 0)
            servers.Add(new CallIceServer(stunUrls));

        var turnUrls = _configuration.GetSection("CallSignaling:Turn:Urls")
            .Get<string[]>() ?? [];
        if (turnUrls.Length == 0) return servers;

        var sharedSecret = _configuration["CallSignaling:Turn:SharedSecret"];
        if (string.IsNullOrWhiteSpace(sharedSecret))
            throw new HubException("TURN is configured without its shared secret.");

        var username = $"{DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()}:{identity.UserId}";
        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(sharedSecret));
        var credential = Convert.ToBase64String(
            hmac.ComputeHash(Encoding.UTF8.GetBytes(username)));
        servers.Add(new CallIceServer(turnUrls, username, credential));
        return servers;
    }

    public async Task JoinThreadPresence(long threadId)
    {
        if (threadId <= 0) throw new HubException("Invalid chat thread.");
        var identity = GetIdentity();
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            ThreadPresenceGroup(threadId));
        lock (StateLock)
        {
            LastSeenByUser[identity.UserId] = DateTimeOffset.UtcNow;
        }
    }

    public async Task<object> GetThreadPresence(long threadId, long participantUserId)
    {
        var identity = GetIdentity();
        var threads = await _chatService.GetThreadsAsync(identity.UserId, identity.Role, Context.ConnectionAborted);
        if (!threads.Any(thread => thread.ThreadId == threadId &&
            thread.ParticipantUserId == participantUserId))
        {
            throw new HubException("The participant is not part of this chat.");
        }

        bool online;
        DateTimeOffset? lastSeen;
        lock (StateLock)
        {
            online = ConnectionsByUser.TryGetValue(participantUserId, out var connections) &&
                     connections.Count > 0;
            LastSeenByUser.TryGetValue(participantUserId, out var seen);
            lastSeen = seen == default ? null : seen;
        }
        await HeartbeatPresence(threadId);
        return new
        {
            online,
            lastSeenAt = lastSeen?.ToString("O"),
        };
    }

    public async Task HeartbeatPresence(long threadId)
    {
        if (threadId <= 0) return;
        var identity = GetIdentity();
        DateTimeOffset seen;
        lock (StateLock)
        {
            seen = DateTimeOffset.UtcNow;
            LastSeenByUser[identity.UserId] = seen;
        }
        await Clients.Group(ThreadPresenceGroup(threadId)).SendAsync(
            "PresenceChanged",
            new { userId = identity.UserId, online = true, lastSeenAt = (string?)null });
    }

    public async Task SetTyping(long threadId, bool typing)
    {
        if (threadId <= 0) return;
        var identity = GetIdentity();
        await Clients.OthersInGroup(ThreadPresenceGroup(threadId)).SendAsync(
            "TypingChanged",
            new { userId = identity.UserId, threadId, typing });
    }

    public override async Task OnConnectedAsync()
    {
        var token = Context.GetHttpContext()?.Request.Query["access_token"].ToString();
        if (!_tokens.TryValidate(token, out var identity) || identity is null)
        {
            Context.Abort();
            return;
        }

        Context.Items["call-identity"] = identity;
        lock (StateLock)
        {
            if (!ConnectionsByUser.TryGetValue(identity.UserId, out var connections))
            {
                connections = [];
                ConnectionsByUser.Add(identity.UserId, connections);
            }
            connections.Add(Context.ConnectionId);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(identity.UserId));
        await base.OnConnectedAsync();
    }

    public async Task<string> StartCall(
        string callId,
        long threadId,
        long targetUserId,
        string mode,
        CallSessionDescription offer)
    {
        var caller = GetIdentity();
        if (caller.UserId == targetUserId)
            throw new HubException("You cannot call yourself.");
        if (!Guid.TryParseExact(callId, "N", out _) ||
            threadId <= 0 ||
            targetUserId <= 0 ||
            mode is not ("audio" or "video") ||
            offer.Type != "offer" ||
            string.IsNullOrWhiteSpace(offer.Sdp) ||
            offer.Sdp.Length > MaxSessionDescriptionLength)
        {
            throw new HubException("The call request is invalid.");
        }

        var callerUser = await _users.GetByIdAsync(caller.UserId, Context.ConnectionAborted);
        var targetUser = await _users.GetByIdAsync(targetUserId, Context.ConnectionAborted);
        if (callerUser is null || !callerUser.IsActive ||
            targetUser is null || !targetUser.IsActive ||
            !string.Equals(callerUser.UserRole, caller.Role, StringComparison.OrdinalIgnoreCase))
        {
            throw new HubException("An active chat participant could not be found.");
        }

        var targetRole = targetUser.UserRole?.Trim().ToUpperInvariant();
        if (targetRole is not ("OWNER" or "SUPPLIER" or "CONSUMER"))
            throw new HubException("The selected participant cannot receive calls.");

        var callerThreads = await _chatService.GetThreadsAsync(
            caller.UserId, caller.Role, Context.ConnectionAborted);
        var targetThreads = await _chatService.GetThreadsAsync(
            targetUserId, targetRole, Context.ConnectionAborted);
        var now = DateTime.Now;
        var callerThread = callerThreads.FirstOrDefault(thread =>
            thread.ThreadId == threadId &&
            thread.ParticipantUserId == targetUserId);
        var targetThread = targetThreads.FirstOrDefault(thread =>
            thread.ThreadId == threadId &&
            thread.ParticipantUserId == caller.UserId);
        if (!HasActiveAccess(callerThread, now) || !HasActiveAccess(targetThread, now))
            throw new HubException("Calls are available only in an active, approved chat.");

        var session = new CallSession(
            callId,
            caller.UserId,
            targetUserId,
            Context.ConnectionId);

        lock (StateLock)
        {
            if (!ConnectionsByUser.TryGetValue(targetUserId, out var targetConnections) ||
                targetConnections.Count == 0)
            {
                throw new HubException("This participant is not online for calls.");
            }
            if (ActiveCallByUser.ContainsKey(caller.UserId) ||
                ActiveCallByUser.ContainsKey(targetUserId) ||
                Calls.ContainsKey(callId))
            {
                throw new HubException("One of the participants is already on a call.");
            }

            Calls.Add(callId, session);
            ActiveCallByUser.Add(caller.UserId, callId);
            ActiveCallByUser.Add(targetUserId, callId);
        }

        try
        {
            await Clients.Group(UserGroup(targetUserId)).SendAsync(
                "IncomingCall",
                new
                {
                    callId,
                    fromUserId = callerUser.UserId,
                    fromName = callerUser.FullName ?? "FreshMilk user",
                    fromAvatar = callerUser.ProfileImageUrl,
                    mode,
                    offer,
                },
                Context.ConnectionAborted);
        }
        catch
        {
            await RemoveCallAsync(session, "CallEnded");
            throw;
        }

        return callId;
    }

    public async Task AnswerCall(string callId, CallSessionDescription answer)
    {
        var identity = GetIdentity();
        var session = GetCallForParticipant(callId, identity.UserId);
        if (session.TargetUserId != identity.UserId ||
            answer.Type != "answer" ||
            string.IsNullOrWhiteSpace(answer.Sdp) ||
            answer.Sdp.Length > MaxSessionDescriptionLength)
        {
            throw new HubException("The answer is invalid or this call is no longer available.");
        }

        lock (StateLock)
        {
            if (!Calls.TryGetValue(callId, out var current) ||
                !ReferenceEquals(current, session) ||
                session.TargetConnectionId is not null)
            {
                throw new HubException("This call was already answered or ended.");
            }
            session.TargetConnectionId = Context.ConnectionId;
        }

        await Clients.Group(UserGroup(session.CallerUserId)).SendAsync(
            "CallAnswered",
            new { callId, fromUserId = identity.UserId, answer },
            Context.ConnectionAborted);
        await Clients.Group(UserGroup(session.TargetUserId)).SendAsync(
            "CallAccepted",
            new { callId, acceptedConnectionId = Context.ConnectionId },
            Context.ConnectionAborted);
    }

    public async Task SendIceCandidate(string callId, CallIceCandidate candidate)
    {
        var identity = GetIdentity();
        var session = GetCallForParticipant(callId, identity.UserId);
        if (string.IsNullOrWhiteSpace(candidate.Candidate) ||
            candidate.Candidate.Length > MaxIceCandidateLength)
        {
            throw new HubException("The ICE candidate is invalid.");
        }

        var destination = identity.UserId == session.CallerUserId
            ? session.TargetUserId
            : session.CallerUserId;
        await Clients.Group(UserGroup(destination)).SendAsync(
            "IceCandidate",
            new { callId, candidate },
            Context.ConnectionAborted);
    }

    public async Task RejectCall(string callId) =>
        await EndCallAsync(callId, "CallRejected");

    public async Task EndCall(string callId) =>
        await EndCallAsync(callId, "CallEnded");

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var identity = Context.Items["call-identity"] as CallAccessIdentity;
        if (identity is not null)
        {
            List<CallSession> ended;
            lock (StateLock)
            {
                if (ConnectionsByUser.TryGetValue(identity.UserId, out var connections))
                {
                    connections.Remove(Context.ConnectionId);
                    if (connections.Count == 0)
                    {
                        ConnectionsByUser.Remove(identity.UserId);
                        ended = Calls.Values
                            .Where(call =>
                                call.CallerUserId == identity.UserId ||
                                call.TargetUserId == identity.UserId)
                            .ToList();
                        foreach (var call in ended) RemoveCallLocked(call);
                    }
                    else
                    {
                        ended = [];
                    }
                }
                else
                {
                    ended = [];
                }
            }

            foreach (var call in ended)
            {
                await Clients.Group(UserGroup(
                    call.CallerUserId == identity.UserId
                        ? call.TargetUserId
                        : call.CallerUserId))
                    .SendAsync("CallEnded", new { callId = call.CallId });
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task EndCallAsync(string callId, string eventName)
    {
        var identity = GetIdentity();
        var session = GetCallForParticipant(callId, identity.UserId);
        await RemoveCallAsync(session, eventName);
    }

    private async Task RemoveCallAsync(CallSession session, string eventName)
    {
        var removed = false;
        lock (StateLock)
        {
            if (Calls.TryGetValue(session.CallId, out var current) &&
                ReferenceEquals(current, session))
            {
                RemoveCallLocked(session);
                removed = true;
            }
        }

        if (!removed) return;
        await Clients.Group(UserGroup(session.CallerUserId)).SendAsync(
            eventName, new { callId = session.CallId });
        await Clients.Group(UserGroup(session.TargetUserId)).SendAsync(
            eventName, new { callId = session.CallId });
    }

    private static void RemoveCallLocked(CallSession session)
    {
        Calls.Remove(session.CallId);
        ActiveCallByUser.Remove(session.CallerUserId);
        ActiveCallByUser.Remove(session.TargetUserId);
    }

    private CallSession GetCallForParticipant(string callId, long userId)
    {
        CallSession? session;
        lock (StateLock)
        {
            Calls.TryGetValue(callId, out session);
        }

        if (!Guid.TryParseExact(callId, "N", out _) ||
            session is null ||
            (session.CallerUserId != userId && session.TargetUserId != userId))
        {
            throw new HubException("This call is no longer available.");
        }
        return session;
    }

    private CallAccessIdentity GetIdentity() =>
        Context.Items["call-identity"] as CallAccessIdentity
        ?? throw new HubException("Sign in again before making a call.");

    private static bool HasActiveAccess(ChatThreadResponse? thread, DateTime now) =>
        thread is not null &&
        thread.CanSend &&
        (!thread.AccessStartsAt.HasValue || thread.AccessStartsAt.Value <= now) &&
        (!thread.AccessEndsAt.HasValue || thread.AccessEndsAt.Value > now);

    private static string UserGroup(long userId) => $"call-user:{userId}";
    private static string ThreadPresenceGroup(long threadId) => $"chat-thread:{threadId}";

    public sealed record CallSessionDescription(string Type, string Sdp);
    public sealed record CallIceServer(
        string[] Urls,
        string? Username = null,
        string? Credential = null);
    public sealed record CallIceCandidate(
        string Candidate,
        string? SdpMid,
        int? SdpMLineIndex,
        string? UsernameFragment);

    private sealed class CallSession(
        string callId,
        long callerUserId,
        long targetUserId,
        string callerConnectionId)
    {
        public string CallId { get; } = callId;
        public long CallerUserId { get; } = callerUserId;
        public long TargetUserId { get; } = targetUserId;
        public string CallerConnectionId { get; } = callerConnectionId;
        public string? TargetConnectionId { get; set; }
    }
}
