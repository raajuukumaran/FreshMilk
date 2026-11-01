namespace FreshMilk.API.Responses.Chat;

public sealed class ChatMessageResponse
{
    public long MessageId { get; init; }
    public long ThreadId { get; init; }
    public long SenderUserId { get; init; }
    public string SenderName { get; init; } = "";
    public long RecipientUserId { get; init; }
    public long OwnerUserId { get; init; }
    public string ThreadKind { get; init; } = "OWNER";
    public string MessageText { get; init; } = "";
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ReadAt { get; init; }
}