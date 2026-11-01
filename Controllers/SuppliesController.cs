using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using FreshMilk.API.Models.Supplies;
using FreshMilk.API.Services.Interfaces;

namespace FreshMilk.API.Controllers;

[ApiController]
[Route("api/supplies")]
public sealed class SuppliesController : ControllerBase
{
    private readonly ISuppliesService _service;

    public SuppliesController(ISuppliesService service)
    {
        _service = service;
    }

    private ObjectResult DatabaseError(SqlException ex)
    {
        var status = ex.Number is >= 50100 and <= 50199
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status500InternalServerError;

        return StatusCode(status, new
        {
            success = false,
            message = status == 400
                ? ex.Message
                : "A database error occurred."
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] long actorUserId,
        [FromQuery] string actorRole = "Owner",
        [FromQuery] DateTime? date = null,
        CancellationToken ct = default)
    {
        try
        {
            var data = await _service.GetAllAsync(
                actorUserId, actorRole, date, ct);

            return Ok(new
            {
                success = true,
                count = data.Count,
                data
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (SqlException ex)
        {
            return DatabaseError(ex);
        }
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(
        long id,
        [FromQuery] long actorUserId,
        [FromQuery] string actorRole = "Owner",
        CancellationToken ct = default)
    {
        try
        {
            var data = await _service.GetByIdAsync(
                id, actorUserId, actorRole, ct);

            return data is null
                ? NotFound(new { success = false, message = "Supply not found." })
                : Ok(new { success = true, data });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (SqlException ex)
        {
            return DatabaseError(ex);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] SaveSupplyRequest request,
        [FromQuery] long actorUserId,
        [FromQuery] string actorRole = "Owner",
        CancellationToken ct = default)
    {
        try
        {
            var data = await _service.CreateAsync(
                request, actorUserId, actorRole, ct);

            return data is null
                ? StatusCode(500, new
                    { success = false, message = "Supply was not created." })
                : CreatedAtAction(
                    nameof(GetById),
                    new { id = data.SupplyId, actorUserId, actorRole },
                    new
                    {
                        success = true,
                        message = "Supply recorded. Awaiting supplier confirmation.",
                        data
                    });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (SqlException ex)
        {
            return DatabaseError(ex);
        }
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] SaveSupplyRequest request,
        [FromQuery] long actorUserId,
        [FromQuery] string actorRole = "Owner",
        CancellationToken ct = default)
    {
        try
        {
            var data = await _service.UpdateAsync(
                id, request, actorUserId, actorRole, ct);

            return data is null
                ? NotFound(new { success = false, message = "Supply not found." })
                : Ok(new
                {
                    success = true,
                    message = "Supply updated. Awaiting supplier confirmation.",
                    data
                });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (SqlException ex)
        {
            return DatabaseError(ex);
        }
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(
        long id,
        [FromQuery] long actorUserId,
        [FromQuery] string actorRole = "Owner",
        CancellationToken ct = default)
    {
        try
        {
            var deleted = await _service.DeleteAsync(
                id, actorUserId, actorRole, ct);

            return deleted
                ? Ok(new { success = true, message = "Supply deleted." })
                : NotFound(new { success = false, message = "Supply not found." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (SqlException ex)
        {
            return DatabaseError(ex);
        }
    }

    [HttpPost("{id:long}/confirm")]
    public async Task<IActionResult> Confirm(
        long id,
        [FromQuery] long actorUserId,
        [FromQuery] string actorRole = "Supplier",
        CancellationToken ct = default)
    {
        try
        {
            var data = await _service.ConfirmAsync(
                id, actorUserId, actorRole, ct);

            return data is null
                ? NotFound(new { success = false, message = "Supply not found." })
                : Ok(new { success = true, message = "Supply confirmed.", data });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (SqlException ex)
        {
            return DatabaseError(ex);
        }
    }

    [HttpPost("{id:long}/request-change")]
    public async Task<IActionResult> RequestChange(
        long id,
        [FromBody] RequestSupplyChange request,
        [FromQuery] long actorUserId,
        [FromQuery] string actorRole = "Supplier",
        CancellationToken ct = default)
    {
        try
        {
            var data = await _service.RequestChangeAsync(
                id, request.ChangeNote, actorUserId, actorRole, ct);

            return data is null
                ? NotFound(new { success = false, message = "Supply not found." })
                : Ok(new
                {
                    success = true,
                    message = "Correction request sent to the owner.",
                    data
                });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (SqlException ex)
        {
            return DatabaseError(ex);
        }
    }
}