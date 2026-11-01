namespace FreshMilk.API.Responses;

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public int? Count { get; init; }
    public T? Data { get; init; }

    public static ApiResponse<T> Ok(T data, string? message = null, int? count = null) =>
        new() { Success = true, Data = data, Message = message, Count = count };

    public static ApiResponse<T> Fail(string message) =>
        new() { Success = false, Message = message };
}