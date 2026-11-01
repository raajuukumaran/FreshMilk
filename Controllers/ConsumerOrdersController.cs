
using FreshMilk.API.Models.ConsumerOrders;
using FreshMilk.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FreshMilk.API.Controllers;

[ApiController]
[Route("api/consumer-orders")]
public sealed class ConsumerOrdersController : ControllerBase
{
    private readonly IConsumerOrdersService _service;

    public ConsumerOrdersController(IConsumerOrdersService service)
    {
        _service = service;
    }

    // POST: api/consumer-orders?actorUserId=2&actorRole=Consumer
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] SaveConsumerOrderRequest request,
        [FromQuery] long actorUserId,
        [FromQuery] string actorRole = "Consumer",
        CancellationToken ct = default)
    {
        var data = await _service.CreateAsync(
            request, actorUserId, actorRole, ct);

        if (data is null)
        {
            return StatusCode(500, new ApiResponse<ConsumerOrderDto>(
                false, "Order was not created.", null));
        }

        return StatusCode(StatusCodes.Status201Created,
            new ApiResponse<ConsumerOrderDto>(
                true, "Order placed successfully.", data));
    }

    // GET: api/consumer-orders/mine?actorUserId=2&actorRole=Consumer
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(
        [FromQuery] long actorUserId,
        [FromQuery] string actorRole = "Consumer",
        CancellationToken ct = default)
    {
        var data = await _service.GetMineAsync(
            actorUserId, actorRole, ct);

        return Ok(new ApiResponse<List<ConsumerOrderDto>>(
            true, "Your orders retrieved successfully.",
            data, data.Count));
    }

    // GET: api/consumer-orders?actorUserId=1&actorRole=Owner
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] long actorUserId,
        [FromQuery] string actorRole = "Owner",
        CancellationToken ct = default)
    {
        var data = await _service.GetAllAsync(
            actorUserId, actorRole, ct);

        return Ok(new ApiResponse<List<ConsumerOrderDto>>(
            true, "Consumer orders retrieved successfully.",
            data, data.Count));
    }

    // PATCH: api/consumer-orders/1/status?actorUserId=1&actorRole=Owner
    [HttpPatch("{id:long}/status")]
    public async Task<IActionResult> UpdateStatus(
        long id,
        [FromBody] UpdateConsumerOrderStatusRequest request,
        [FromQuery] long actorUserId,
        [FromQuery] string actorRole = "Owner",
        CancellationToken ct = default)
    {
        var data = await _service.UpdateStatusAsync(
            id, request, actorUserId, actorRole, ct);

        if (data is null)
        {
            return NotFound(new ApiResponse<ConsumerOrderDto>(
                false, "Order not found.", null));
        }

        return Ok(new ApiResponse<ConsumerOrderDto>(
            true, "Order status updated successfully.", data));
    }
}
