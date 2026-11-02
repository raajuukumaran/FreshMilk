
using System.Text.Json;
using Microsoft.Data.SqlClient;
using FreshMilk.API.Responses;

namespace FreshMilk.API.Middleware;

public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(
        RequestDelegate next,
        ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ArgumentException exception)
        {
            await WriteErrorAsync(
                context,
                StatusCodes.Status400BadRequest,
                exception.Message);
        }
        catch (SqlException exception) when (
            exception.Number is >= 50200 and <= 50299
            or >= 50300 and <= 50399
            or >= 50400 and <= 50499)
        {
            _logger.LogWarning(
                exception,
                "Chat/business validation SQL error {SqlErrorNumber} on {Path}",
                exception.Number,
                context.Request.Path);

            await WriteErrorAsync(
                context,
                StatusCodes.Status400BadRequest,
                exception.Message);
        }
        catch (SqlException exception) when (
            context.Request.Path.StartsWithSegments("/api/chat")
            && exception.Number is 207 or 208 or 2812 or 8144)
        {
            _logger.LogError(
                exception,
                "Chat SQL error {SqlErrorNumber}. Procedure: {Procedure}. Message: {SqlMessage}",
                exception.Number,
                exception.Procedure,
                exception.Message);

            await WriteErrorAsync(
                context,
                StatusCodes.Status503ServiceUnavailable,
                "A chat database SQL error occurred. Check the API terminal logs for details.");
        }
        catch (IndexOutOfRangeException exception) when (
            context.Request.Path.StartsWithSegments("/api/chat"))
        {
            _logger.LogError(
                exception,
                "Chat result mapping failed. A required result column may be missing.");

            await WriteErrorAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "The chat API could not map the database result. Check the API terminal logs.");
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled API request error for {Path}",
                context.Request.Path);

            await WriteErrorAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "An unexpected server error occurred.");
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        string message)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            ApiResponse<string>.Fail(message),
            cancellationToken: context.RequestAborted);
    }
}
