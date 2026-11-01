
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using FreshMilk.API.Models.MilkRates;
using FreshMilk.API.Services.Interfaces;

namespace FreshMilk.API.Controllers;

[ApiController]
[Route("api/milk-rates")]
public sealed class MilkRatesController : ControllerBase
{
    private readonly IMilkRatesService _service;

    public MilkRatesController(IMilkRatesService service)
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

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent(CancellationToken ct)
    {
        var data = await _service.GetCurrentAsync(ct);
        return Ok(new { success = true, count = data.Count, data });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(
        long id, CancellationToken ct)
    {
        var data = await _service.GetByIdAsync(id, ct);

        return data is null
            ? NotFound(new { success = false, message = "Milk rate not found." })
            : Ok(new { success = true, data });
    }

    [HttpPost]
    public async Task<IActionResult> SetRate(
        [FromBody] SaveMilkRateRequest request,
        CancellationToken ct)
    {
        try
        {
            var data = await _service.SetRateAsync(request, ct);

            return data is null
                ? StatusCode(500, new
                    {
                        success = false,
                        message = "The saved rate was not returned."
                    })
                : Ok(new
                    {
                        success = true,
                        message = "Milk rate saved.",
                        data
                    });
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

[HttpPut("{id:long}")]
public async Task<IActionResult> Update(
    long id,
    [FromBody] SaveMilkRateRequest request,
    CancellationToken ct)
{
    if (!ModelState.IsValid)
        return ValidationProblem(ModelState);

    try
    {
        var result = await _service.UpdateAsync(id, request, ct);

        if (result is null)
        {
            return NotFound(new
            {
                success = false,
                message = $"Milk rate {id} was not found."
            });
        }

        return Ok(new
        {
            success = true,
            message = "Milk rate updated successfully.",
            data = result
        });
    }
    catch (ArgumentException ex)
    {
        return BadRequest(new
        {
            success = false,
            message = ex.Message
        });
    }
}
}
