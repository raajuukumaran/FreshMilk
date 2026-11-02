using FreshMilk.API.Models.Chat;
using FreshMilk.API.Responses.Chat;

namespace FreshMilk.API.Services.Interfaces;

public interface IChatService
{
    Task<List<ChatThreadResponse>> GetThreadsAsync(
        long actorUserId, string actorRole, CancellationToken ct);

    Task<ChatThreadResponse?> OpenThreadAsync(
        OpenChatThreadRequest request, long actorUserId, string actorRole,
        CancellationToken ct);

    Task<List<ChatAccessRequestResponse>> GetRequestsAsync(
        long actorUserId, string actorRole, CancellationToken ct);

    Task<ChatAccessRequestResponse?> RequestAccessAsync(
        RequestChatAccessRequest request, long actorUserId, string actorRole,
        CancellationToken ct);

    Task<ChatAccessRequestResponse?> RespondToRequestAsync(
        long requestId, bool accept, long actorUserId, string actorRole,
        CancellationToken ct);

    Task<ChatAccessRequestResponse?> ReviewRequestAsync(
        long requestId, bool approve, DateOnly? accessStartDate,
        DateOnly? accessEndDate,
        long actorUserId, string actorRole,
        CancellationToken ct);

    Task<List<ChatMessageResponse>> GetMessagesAsync(
        long threadId, long actorUserId, string actorRole, CancellationToken ct);

    Task<ChatMessageResponse?> SendMessageAsync(
        long threadId, SendChatMessageRequest request,
        long actorUserId, string actorRole, CancellationToken ct);

    Task<int> MarkReadAsync(
        long threadId, long actorUserId, string actorRole, CancellationToken ct);
}