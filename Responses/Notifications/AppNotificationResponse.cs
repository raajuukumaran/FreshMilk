namespace FreshMilk.API.Responses.Notifications;

public sealed class AppNotificationResponse
{
    public long NotificationId { get; init; }
    public long ToUserId { get; init; }
    public string ToRole { get; init; } = "";
    public string FromName { get; init; } = "FreshMilk";
    public string FromRole { get; init; } = "SYSTEM";
    public string NotificationType { get; init; } = "";
    public string Title { get; init; } = "";
    public string Body { get; init; } = "";
    public long? RelatedSupplyId { get; init; }
    public long? RelatedOrderId { get; init; }
    public long? RelatedThreadId { get; init; }
    public long? RelatedChatRequestId { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
    public int UnreadCount { get; init; }
}