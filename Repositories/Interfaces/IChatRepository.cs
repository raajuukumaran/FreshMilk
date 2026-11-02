using FreshMilk.API.Responses.Chat;
using FreshMilk.API.Models.Chat;

namespace FreshMilk.API.Repositories.Interfaces;

public interface IChatRepository
{
    Task<List<ChatThreadResponse>> GetThreadsAsync(
        long actorUserId, string actorRole, CancellationToken ct);

    Task<ChatThreadResponse?> OpenThreadAsync(
        long actorUserId, string actorRole, string participantRole, long participantUserId,
        long? relatedSupplyId, CancellationToken ct);

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
        long actorUserId, string actorRole, long threadId, CancellationToken ct);

    Task<ChatMessageResponse?> SendMessageAsync(
        long actorUserId, string actorRole, long threadId, string messageText,
        CancellationToken ct);

    Task<int> MarkReadAsync(
        long actorUserId, string actorRole, long threadId, CancellationToken ct);
}