namespace FreshMilk.API.Responses.Chat;

public sealed class ChatAccessRequestResponse
{
    public long RequestId { get; init; }
    public long RequesterUserId { get; init; }
    public string RequesterName { get; init; } = "";
    public string RequesterRole { get; init; } = "";
    public long TargetUserId { get; init; }
    public string TargetName { get; init; } = "";
    public string TargetRole { get; init; } = "";
    public long? RelatedSupplyId { get; init; }
    public DateTime? AccessEndsAt { get; init; }
    public string? RequestNote { get; init; }
    public string RequestStatus { get; init; } = "";
    public long? ThreadId { get; init; }
    public DateTime CreatedAt { get; init; }
    public long? ReviewedByOwnerId { get; init; }
    public DateTime? ReviewedAt { get; init; }
}