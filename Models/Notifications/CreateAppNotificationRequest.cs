namespace FreshMilk.API.Models.Notifications;

public sealed class CreateAppNotificationRequest
{
    public long? ToUserId { get; init; }
    public string ToRole { get; init; } = "";
    public string NotificationType { get; init; } = "";
    public string Title { get; init; } = "";
    public string Body { get; init; } = "";
    public long? RelatedSupplyId { get; init; }
    public long? RelatedOrderId { get; init; }
    public long? RelatedThreadId { get; init; }
    public long? RelatedChatRequestId { get; init; }
}