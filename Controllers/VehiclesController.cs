
using DriveHub.Services;
using Microsoft.AspNetCore.Mvc;

namespace DriveHub.Controllers;

[Route("api/vehicles")]
public class VehiclesController(
    VehicleApiService vehicleService) : Controller
{
    // GET /api/vehicles/makes
    [HttpGet("makes")]
    public async Task<IActionResult> GetMakes()
    {
        try
        {
            var makes = await vehicleService.GetMakesAsync();
            return Ok(makes);
        }
        catch (HttpRequestException)
        {
            return StatusCode(503,
                new { error = "Vehicle API unavailable." });
        }
        catch (TaskCanceledException)
        {
            return StatusCode(504,
                new { error = "Vehicle API timed out." });
        }
    }

    // GET /api/vehicles/models?makeId=448&year=2020
    [HttpGet("models")]
    public async Task<IActionResult> GetModels(
        int makeId,
        int year)
    {
        if (makeId <= 0 ||
            year < 1996 ||
            year > DateTime.UtcNow.Year + 2)
        {
            return BadRequest(
                new { error = "Invalid make or year." });
        }

        try
        {
            var models = await vehicleService
                .GetModelsAsync(makeId, year);

            return Ok(models);
        }
        catch (HttpRequestException)
        {
            return StatusCode(503,
                new { error = "Vehicle API unavailable." });
        }
        catch (TaskCanceledException)
        {
            return StatusCode(504,
                new { error = "Vehicle API timed out." });
        }
    }
}
