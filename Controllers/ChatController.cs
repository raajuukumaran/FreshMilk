using Microsoft.AspNetCore.Mvc;
using FreshMilk.API.Models.Chat;
using FreshMilk.API.Responses;
using FreshMilk.API.Responses.Chat;
using FreshMilk.API.Services.Interfaces;

namespace FreshMilk.API.Controllers;

[ApiController]
[Route("api/chat")]
public sealed class ChatController : ControllerBase
{
    private readonly IChatService _service;

    public ChatController(IChatService service) => _service = service;

    [HttpGet("threads")]
    public async Task<ActionResult<ApiResponse<List<ChatThreadResponse>>>> GetThreads(
        [FromQuery] long actorUserId,
        [FromQuery] string actorRole,
        CancellationToken ct)
    {
        var threads = await _service.GetThreadsAsync(actorUserId, actorRole, ct);
        return Ok(ApiResponse<List<ChatThreadResponse>>.Ok(threads, count: threads.Count));
    }

    [HttpPost("threads")]
    public async Task<ActionResult<ApiResponse<ChatThreadResponse>>> OpenThread(
        [FromBody] OpenChatThreadRequest request,
        [FromQuery] long actorUserId,
        [FromQuery] string actorRole,
        CancellationToken ct)
    {
        var thread = await _service.OpenThreadAsync(request, actorUserId, actorRole, ct);
        return thread is null
            ? NotFound(ApiResponse<ChatThreadResponse>.Fail("Chat participant not found."))
            : Ok(ApiResponse<ChatThreadResponse>.Ok(thread));
    }

    [HttpGet("requests")]
    public async Task<ActionResult<ApiResponse<List<ChatAccessRequestResponse>>>> GetRequests(
        [FromQuery] long actorUserId,
        [FromQuery] string actorRole,
        CancellationToken ct)
    {
        var requests = await _service.GetRequestsAsync(actorUserId, actorRole, ct);
        return Ok(ApiResponse<List<ChatAccessRequestResponse>>.Ok(
            requests, count: requests.Count));
    }

    [HttpPost("requests")]
    public async Task<ActionResult<ApiResponse<ChatAccessRequestResponse>>> RequestAccess(
        [FromBody] RequestChatAccessRequest request,
        [FromQuery] long actorUserId,
        [FromQuery] string actorRole,
        CancellationToken ct)
    {
        var created = await _service.RequestAccessAsync(
            request, actorUserId, actorRole, ct);
        return created is null
            ? StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<ChatAccessRequestResponse>.Fail("Chat request was not saved."))
            : Ok(ApiResponse<ChatAccessRequestResponse>.Ok(
                created, "Chat request sent to the selected user."));
    }

    [HttpPost("requests/{requestId:long}/accept")]
    public async Task<ActionResult<ApiResponse<ChatAccessRequestResponse>>> AcceptRequest(
        long requestId,
        [FromQuery] long actorUserId,
        [FromQuery] string actorRole,
        CancellationToken ct)
    {
        var responded = await _service.RespondToRequestAsync(
            requestId, true, actorUserId, actorRole, ct);
        return responded is null
            ? NotFound(ApiResponse<ChatAccessRequestResponse>.Fail("Pending request for this user not found."))
            : Ok(ApiResponse<ChatAccessRequestResponse>.Ok(
                responded, "Chat request accepted and sent to the owner for approval."));
    }

    [HttpPost("requests/{requestId:long}/decline")]
    public async Task<ActionResult<ApiResponse<ChatAccessRequestResponse>>> DeclineRequest(
        long requestId,
        [FromQuery] long actorUserId,
        [FromQuery] string actorRole,
        CancellationToken ct)
    {
        var responded = await _service.RespondToRequestAsync(
            requestId, false, actorUserId, actorRole, ct);
        return responded is null
            ? NotFound(ApiResponse<ChatAccessRequestResponse>.Fail("Pending request for this user not found."))
            : Ok(ApiResponse<ChatAccessRequestResponse>.Ok(
                responded, "Chat request declined."));
    }

    [HttpPost("requests/{requestId:long}/approve")]
    public async Task<ActionResult<ApiResponse<ChatAccessRequestResponse>>> ApproveRequest(
        long requestId,
        [FromBody] ReviewChatRequestRequest request,
        [FromQuery] long actorUserId,
        [FromQuery] string actorRole,
        CancellationToken ct)
    {
        var reviewed = await _service.ReviewRequestAsync(
            requestId, true, request.AccessMinutes, actorUserId, actorRole, ct);
        return reviewed is null
            ? NotFound(ApiResponse<ChatAccessRequestResponse>.Fail("Pending chat request not found."))
            : Ok(ApiResponse<ChatAccessRequestResponse>.Ok(reviewed, "Chat request approved."));
    }

    [HttpPost("requests/{requestId:long}/reject")]
    public async Task<ActionResult<ApiResponse<ChatAccessRequestResponse>>> RejectRequest(
        long requestId,
        [FromQuery] long actorUserId,
        [FromQuery] string actorRole,
        CancellationToken ct)
    {
        var reviewed = await _service.ReviewRequestAsync(
            requestId, false, 60, actorUserId, actorRole, ct);
        return reviewed is null
            ? NotFound(ApiResponse<ChatAccessRequestResponse>.Fail("Pending chat request not found."))
            : Ok(ApiResponse<ChatAccessRequestResponse>.Ok(reviewed, "Chat request declined."));
    }

    [HttpGet("threads/{threadId:long}/messages")]
    public async Task<ActionResult<ApiResponse<List<ChatMessageResponse>>>> GetMessages(
        long threadId,
        [FromQuery] long actorUserId,
        [FromQuery] string actorRole,
        CancellationToken ct)
    {
        var messages = await _service.GetMessagesAsync(threadId, actorUserId, actorRole, ct);
        return Ok(ApiResponse<List<ChatMessageResponse>>.Ok(messages, count: messages.Count));
    }

    [HttpPost("threads/{threadId:long}/messages")]
    public async Task<ActionResult<ApiResponse<ChatMessageResponse>>> SendMessage(
        long threadId,
        [FromBody] SendChatMessageRequest request,
        [FromQuery] long actorUserId,
        [FromQuery] string actorRole,
        CancellationToken ct)
    {
        var message = await _service.SendMessageAsync(
            threadId, request, actorUserId, actorRole, ct);
        return message is null
            ? NotFound(ApiResponse<ChatMessageResponse>.Fail("Chat thread not found."))
            : Ok(ApiResponse<ChatMessageResponse>.Ok(message, "Message sent."));
    }

    [HttpPost("threads/{threadId:long}/read")]
    public async Task<ActionResult<ApiResponse<int>>> MarkRead(
        long threadId,
        [FromQuery] long actorUserId,
        [FromQuery] string actorRole,
        CancellationToken ct)
    {
        var count = await _service.MarkReadAsync(threadId, actorUserId, actorRole, ct);
        return Ok(ApiResponse<int>.Ok(count, "Messages marked as read."));
    }
}