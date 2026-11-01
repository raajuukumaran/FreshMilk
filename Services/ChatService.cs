using FreshMilk.API.Models.Chat;
using FreshMilk.API.Models.Notifications;
using FreshMilk.API.Repositories.Interfaces;
using FreshMilk.API.Responses.Chat;
using FreshMilk.API.Services.Interfaces;

namespace FreshMilk.API.Services;

internal static class ChatRequestLabels
{
    public static bool IsExtension(string? note) =>
        note?.Trim().StartsWith(
            "Access extension requested.",
            StringComparison.OrdinalIgnoreCase) == true;
}

public sealed class ChatService : IChatService
{
    private readonly IChatRepository _repository;
    private readonly IAppNotificationsService _notifications;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IChatRepository repository,
        IAppNotificationsService notifications,
        ILogger<ChatService> logger)
    {
        _repository = repository;
        _notifications = notifications;
        _logger = logger;
    }

    private static string ValidateActor(long actorUserId, string actorRole)
    {
        if (actorUserId <= 0) throw new ArgumentException("Valid actor user ID is required.");
        var role = actorRole.Trim().ToUpperInvariant();
        if (role is not ("OWNER" or "SUPPLIER" or "CONSUMER"))
            throw new ArgumentException("Role must be Owner, Supplier, or Consumer.");
        return role;
    }

    public Task<List<ChatThreadResponse>> GetThreadsAsync(
        long actorUserId, string actorRole, CancellationToken ct) =>
        _repository.GetThreadsAsync(actorUserId, ValidateActor(actorUserId, actorRole), ct);

    public Task<ChatThreadResponse?> OpenThreadAsync(
        OpenChatThreadRequest request, long actorUserId, string actorRole,
        CancellationToken ct)
    {
        var role = ValidateActor(actorUserId, actorRole);
        if (request.ParticipantUserId <= 0)
            throw new ArgumentException("Valid participant user ID is required.");
        var participantRole = request.ParticipantRole.Trim().ToUpperInvariant();
        var directOwnerChat = role != "OWNER" && participantRole == "OWNER";
        if (role == "OWNER" && participantRole is not ("SUPPLIER" or "CONSUMER"))
            throw new ArgumentException("Owners can start direct chats with suppliers and consumers.");
        if (role != "OWNER" && !directOwnerChat)
            throw new ArgumentException("Peer chats require owner approval.");
        if (request.ParticipantUserId == actorUserId)
            throw new ArgumentException("You cannot start a chat with yourself.");
        return _repository.OpenThreadAsync(
            actorUserId, role, participantRole, request.ParticipantUserId,
            request.RelatedSupplyId, ct);
    }

    public Task<List<ChatAccessRequestResponse>> GetRequestsAsync(
        long actorUserId, string actorRole, CancellationToken ct) =>
        _repository.GetRequestsAsync(actorUserId, ValidateActor(actorUserId, actorRole), ct);

    public async Task<ChatAccessRequestResponse?> RequestAccessAsync(
        RequestChatAccessRequest request, long actorUserId, string actorRole,
        CancellationToken ct)
    {
        var role = ValidateActor(actorUserId, actorRole);
        if (role == "OWNER")
            throw new ArgumentException("Owners can start chats directly.");
        if (request.TargetUserId <= 0 || request.TargetUserId == actorUserId)
            throw new ArgumentException("Choose a different active user.");
        var targetRole = request.TargetRole.Trim().ToUpperInvariant();
        if (targetRole is not ("SUPPLIER" or "CONSUMER"))
            throw new ArgumentException("Chat requests must target a supplier or consumer.");
        if (request.RelatedSupplyId is <= 0)
            throw new ArgumentException("Invalid related supply ID.");

        request = new RequestChatAccessRequest
        {
            TargetUserId = request.TargetUserId,
            TargetRole = targetRole,
            RelatedSupplyId = request.RelatedSupplyId,
            RequestNote = request.RequestNote?.Trim(),
        };
        var created = await _repository.RequestAccessAsync(request, actorUserId, role, ct);
        if (created is not null)
        {
            var isExtension = ChatRequestLabels.IsExtension(created.RequestNote);
            await TryNotifyAsync(new CreateAppNotificationRequest
            {
                ToUserId = created.TargetUserId,
                ToRole = created.TargetRole,
                NotificationType = "CHAT_ACCESS_REQUEST",
                Title = isExtension ? "Chat extension request" : "New chat request",
                Body = isExtension
                    ? $"{created.RequesterName} is requesting renewed access to your previous chat."
                    : $"{created.RequesterName} wants to chat with you.",
                RelatedChatRequestId = created.RequestId,
            }, actorUserId, role, ct);
        }
        return created;
    }

    public async Task<ChatAccessRequestResponse?> RespondToRequestAsync(
        long requestId, bool accept, long actorUserId, string actorRole,
        CancellationToken ct)
    {
        var role = ValidateActor(actorUserId, actorRole);
        if (role == "OWNER")
            throw new ArgumentException("Only the requested supplier or consumer can respond first.");
        if (requestId <= 0) throw new ArgumentException("Invalid chat request ID.");

        var response = await _repository.RespondToRequestAsync(
            requestId, accept, actorUserId, role, ct);
        if (response is null) return null;

        if (accept)
        {
            var isExtension = ChatRequestLabels.IsExtension(response.RequestNote);
            await TryNotifyAsync(new CreateAppNotificationRequest
            {
                ToUserId = response.RequesterUserId,
                ToRole = response.RequesterRole,
                NotificationType = "CHAT_ACCESS_TARGET_ACCEPTED",
                Title = isExtension ? "Chat extension accepted" : "Chat request accepted",
                Body = isExtension
                    ? $"{response.TargetName} accepted your extension request. It is waiting for owner approval."
                    : $"{response.TargetName} accepted your request. It is waiting for owner approval.",
                RelatedChatRequestId = response.RequestId,
            }, actorUserId, role, ct);

            await TryNotifyAsync(new CreateAppNotificationRequest
            {
                ToRole = "OWNER",
                NotificationType = "CHAT_ACCESS_REQUEST",
                Title = isExtension ? "Chat extension needs your approval" : "Chat request needs your approval",
                Body = isExtension
                    ? $"{response.RequesterName} and {response.TargetName} accepted an extension request. Choose a new access window."
                    : $"{response.RequesterName} and {response.TargetName} are ready to chat.",
                RelatedChatRequestId = response.RequestId,
            }, actorUserId, role, ct);
        }
        else
        {
            await TryNotifyAsync(new CreateAppNotificationRequest
            {
                ToUserId = response.RequesterUserId,
                ToRole = response.RequesterRole,
                NotificationType = "CHAT_ACCESS_REJECTED",
                Title = "Chat request declined",
                Body = $"{response.TargetName} declined your chat request.",
                RelatedChatRequestId = response.RequestId,
            }, actorUserId, role, ct);
        }

        return response;
    }

    public async Task<ChatAccessRequestResponse?> ReviewRequestAsync(
        long requestId, bool approve, int accessMinutes,
        long actorUserId, string actorRole,
        CancellationToken ct)
    {
        if (ValidateActor(actorUserId, actorRole) != "OWNER")
            throw new ArgumentException("Only an owner can review chat requests.");
        if (requestId <= 0) throw new ArgumentException("Invalid chat request ID.");
        if (approve && (accessMinutes < 5 || accessMinutes > 1440))
            throw new ArgumentException("Peer chat access must be between 5 minutes and 24 hours.");

        var reviewed = await _repository.ReviewRequestAsync(
            requestId, approve, accessMinutes, actorUserId, "OWNER", ct);
        if (reviewed is null) return null;

        var isExtension = ChatRequestLabels.IsExtension(reviewed.RequestNote);
        var body = approve
            ? isExtension
                ? $"The owner extended your chat access with {reviewed.TargetName}."
                : $"The owner approved your chat with {reviewed.TargetName}."
            : isExtension
                ? $"The owner declined your chat extension request with {reviewed.TargetName}."
                : $"The owner declined your chat with {reviewed.TargetName}.";
        foreach (var recipient in new[]
        {
            (reviewed.RequesterUserId, reviewed.RequesterRole),
            (reviewed.TargetUserId, reviewed.TargetRole),
        })
        {
            await TryNotifyAsync(new CreateAppNotificationRequest
            {
                ToUserId = recipient.Item1,
                ToRole = recipient.Item2,
                NotificationType = approve ? "CHAT_ACCESS_APPROVED" : "CHAT_ACCESS_REJECTED",
                Title = approve
                    ? isExtension ? "Chat access extended" : "Chat approved"
                    : isExtension ? "Chat extension declined" : "Chat request declined",
                Body = body,
                RelatedThreadId = reviewed.ThreadId,
                RelatedChatRequestId = reviewed.RequestId,
            }, actorUserId, "OWNER", ct);
        }
        return reviewed;
    }

    public Task<List<ChatMessageResponse>> GetMessagesAsync(
        long threadId, long actorUserId, string actorRole, CancellationToken ct)
    {
        if (threadId <= 0) throw new ArgumentException("Invalid chat thread ID.");
        return _repository.GetMessagesAsync(
            actorUserId, ValidateActor(actorUserId, actorRole), threadId, ct);
    }

    public async Task<ChatMessageResponse?> SendMessageAsync(
        long threadId, SendChatMessageRequest request,
        long actorUserId, string actorRole, CancellationToken ct)
    {
        if (threadId <= 0) throw new ArgumentException("Invalid chat thread ID.");
        var message = request.MessageText.Trim();
        if (message.Length == 0) throw new ArgumentException("Message text is required.");
        if (message.Length > 2000) throw new ArgumentException("Message must be 2000 characters or fewer.");
        var role = ValidateActor(actorUserId, actorRole);
        var sent = await _repository.SendMessageAsync(
            actorUserId, role, threadId, message, ct);
        if (sent is not null)
        {
            var preview = sent.MessageText.Length > 450
                ? $"{sent.MessageText[..447]}..."
                : sent.MessageText;
            try
            {
                await _notifications.CreateAsync(new CreateAppNotificationRequest
                {
                    ToUserId = sent.RecipientUserId,
                    NotificationType = "CHAT_MESSAGE",
                    Title = $"New message from {sent.SenderName}",
                    Body = preview,
                    RelatedThreadId = sent.ThreadId,
                }, actorUserId, role, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not notify recipient for chat thread {ThreadId}", threadId);
            }

            if (sent.ThreadKind == "PEER" && sent.OwnerUserId != actorUserId)
            {
                await TryNotifyAsync(new CreateAppNotificationRequest
                {
                    ToUserId = sent.OwnerUserId,
                    ToRole = "OWNER",
                    NotificationType = "PEER_CHAT_ACTIVITY",
                    Title = "Peer chat activity",
                    Body = $"{sent.SenderName} sent a message in an owner-approved conversation.",
                    RelatedThreadId = sent.ThreadId,
                }, actorUserId, role, ct);
            }
        }
        return sent;
    }

    private async Task TryNotifyAsync(
        CreateAppNotificationRequest request,
        long actorUserId, string actorRole, CancellationToken ct)
    {
        try
        {
            await _notifications.CreateAsync(request, actorUserId, actorRole, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create chat workflow notification");
        }
    }

    public Task<int> MarkReadAsync(
        long threadId, long actorUserId, string actorRole, CancellationToken ct)
    {
        if (threadId <= 0) throw new ArgumentException("Invalid chat thread ID.");
        return _repository.MarkReadAsync(
            actorUserId, ValidateActor(actorUserId, actorRole), threadId, ct);
    }
}