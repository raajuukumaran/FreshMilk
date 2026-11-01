namespace FreshMilk.API.Responses.Chat;

public sealed class ChatThreadResponse
{
    public long ThreadId { get; init; }
    public long ParticipantUserId { get; init; }
    public string ParticipantName { get; init; } = "";
    public string ParticipantRole { get; init; } = "";
    public string ThreadKind { get; init; } = "OWNER";
    public long? RelatedSupplyId { get; init; }
    public DateTime? AccessEndsAt { get; init; }
    public bool CanSend { get; init; } = true;
    public string? LastMessage { get; init; }
    public DateTime? LastMessageAt { get; init; }
    public int UnreadCount { get; init; }
}