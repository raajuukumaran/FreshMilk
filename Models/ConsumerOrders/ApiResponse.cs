
namespace FreshMilk.API.Models.ConsumerOrders;

public sealed record ApiResponse<T>(
    bool Success,
    string Message,
    T? Data,
    int? Count = null);
