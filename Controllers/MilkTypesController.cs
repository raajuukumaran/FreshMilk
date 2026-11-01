
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using FreshMilk.API.Models.MilkTypes;
using FreshMilk.API.Services.Interfaces;

namespace FreshMilk.API.Controllers;

[ApiController]
[Route("api/milk-types")]
public sealed class MilkTypesController : ControllerBase
{
    private readonly IMilkTypesService _service;

    public MilkTypesController(IMilkTypesService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var data = await _service.GetAllAsync(includeInactive, ct);
        return Ok(new { success = true, count = data.Count, data });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        int id, CancellationToken ct)
    {
        var data = await _service.GetByIdAsync(id, ct);

        return data is null
            ? NotFound(new { success = false, message = "Milk type not found." })
            : Ok(new { success = true, data });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] SaveMilkTypeRequest request,
        CancellationToken ct)
    {
        try
        {
            var data = await _service.CreateAsync(request, ct);
            return CreatedAtAction(
                nameof(GetById), new { id = data.MilkTypeId },
                new { success = true, message = "Milk type created.", data });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id, [FromBody] SaveMilkTypeRequest request,
        CancellationToken ct)
    {
        try
        {
            var data = await _service.UpdateAsync(id, request, ct);

            return data is null
                ? NotFound(new { success = false, message = "Milk type not found." })
                : Ok(new { success = true, message = "Milk type updated.", data });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/activate")]
    public Task<IActionResult> Activate(int id, CancellationToken ct) =>
        SetActive(id, true, ct);

    [HttpPatch("{id:int}/deactivate")]
    public Task<IActionResult> Deactivate(int id, CancellationToken ct) =>
        SetActive(id, false, ct);

    private async Task<IActionResult> SetActive(
        int id, bool active, CancellationToken ct)
    {
        try
        {
            var data = await _service.SetActiveAsync(id, active, ct);

            return data is null
                ? NotFound(new { success = false, message = "Milk type not found." })
                : Ok(new { success = true, data });
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
    }
}
