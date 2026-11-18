using DriveHub.Models;
using DriveHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DriveHub.Controllers;

public class PartsController(
    PartRepository repo,
    VehicleApiService vehicleApi) : Controller
{
    // PUBLIC MARKETPLACE
    public async Task<IActionResult> Index(
        string? search,
        PartType? category,
        int? vehicleYear,
        int? vehicleMakeId,
        int? vehicleModelId)
    {
        var parts = await repo.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            parts = parts.Where(part =>
                part.Name.Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase) ||
                part.Manufacturer.Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase) ||
                part.PartNumber.Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        if (category.HasValue)
        {
            parts = parts.Where(part =>
                part.Category == category.Value
            ).ToList();
        }

        if (vehicleYear.HasValue)
        {
            parts = parts.Where(part =>
                part.VehicleYear == vehicleYear.Value
            ).ToList();
        }

        if (vehicleMakeId.HasValue)
        {
            parts = parts.Where(part =>
                part.VehicleMakeId == vehicleMakeId.Value
            ).ToList();
        }

        if (vehicleModelId.HasValue)
        {
            parts = parts.Where(part =>
                part.VehicleModelId == vehicleModelId.Value
            ).ToList();
        }

        ViewBag.Search = search;
        ViewBag.Category = category;
        ViewBag.VehicleYear = vehicleYear;
        ViewBag.VehicleMakeId = vehicleMakeId;
        ViewBag.VehicleModelId = vehicleModelId;

        return View(parts);
    }

    // PUBLIC PART DETAILS
    public async Task<IActionResult> Details(string id)
    {
        var part = await repo.GetAsync(id);

        return part == null
            ? NotFound()
            : View(part);
    }

    // SELLER DASHBOARD
    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> Manage(
        string? search,
        PartType? category,
        string? stock)
    {
        var sellerId = GetSellerId();

        if (sellerId == null)
            return Forbid();

        var allParts = await repo.GetBySellerAsync(sellerId);

        ViewBag.TotalParts = allParts.Count;

        ViewBag.InventoryValue = allParts.Sum(
            part => part.Price * part.Quantity);

        ViewBag.LowStock = allParts.Count(
            part => part.Quantity > 0 &&
                    part.Quantity <= 5);

        ViewBag.OutOfStock = allParts.Count(
            part => part.Quantity == 0);

        var parts = allParts.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            parts = parts.Where(part =>
                part.Name.Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase) ||
                part.Manufacturer.Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase) ||
                part.PartNumber.Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase)
            );
        }

        if (category.HasValue)
        {
            parts = parts.Where(part =>
                part.Category == category.Value);
        }

        if (stock == "low")
        {
            parts = parts.Where(part =>
                part.Quantity > 0 &&
                part.Quantity <= 5);
        }
        else if (stock == "out")
        {
            parts = parts.Where(part =>
                part.Quantity == 0);
        }
        else if (stock == "available")
        {
            parts = parts.Where(part =>
                part.Quantity > 0);
        }

        ViewBag.Search = search;
        ViewBag.Category = category;
        ViewBag.Stock = stock;

        return View(parts.ToList());
    }

    // CREATE - GET
    [Authorize(Roles = "Seller")]
    [HttpGet]
    public IActionResult Create()
    {
        return View(new CarPart());
    }

    // CREATE - POST
    [Authorize(Roles = "Seller")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Name,Manufacturer,PartNumber,Category,Condition,Grade,QualityScore,Price,Quantity,WarrantyMonths,MileageKm,Description,VehicleYear,VehicleMakeId,VehicleModelId")]
        CarPart part)
    {
        var sellerId = GetSellerId();

        if (sellerId == null)
            return Forbid();

        if (!ModelState.IsValid)
            return View(part);

        if (!await ValidateVehicleAsync(part))
            return View(part);

        await repo.AddForSellerAsync(part, sellerId);

        return RedirectToAction(nameof(Manage));
    }

    // EDIT - GET
    [Authorize(Roles = "Seller")]
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var sellerId = GetSellerId();

        if (sellerId == null)
            return Forbid();

        var part = await repo.GetAsync(id);

        if (part == null)
            return NotFound();

        if (part.SellerId != sellerId)
            return Forbid();

        return View(part);
    }

    // EDIT - POST
    [Authorize(Roles = "Seller")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string id,
        [Bind("Name,Manufacturer,PartNumber,Category,Condition,Grade,QualityScore,Price,Quantity,WarrantyMonths,MileageKm,Description,VehicleYear,VehicleMakeId,VehicleModelId")]
        CarPart part)
    {
        var sellerId = GetSellerId();

        if (sellerId == null)
            return Forbid();

        part.Id = id;

        if (!ModelState.IsValid)
            return View(part);

        if (!await ValidateVehicleAsync(part))
            return View(part);

        var updated = await repo.UpdateForSellerAsync(
            part,
            sellerId);

        if (!updated)
            return NotFound();

        return RedirectToAction(nameof(Manage));
    }

    // DELETE - GET
    [Authorize(Roles = "Seller")]
    [HttpGet]
    public async Task<IActionResult> Delete(string id)
    {
        var sellerId = GetSellerId();

        if (sellerId == null)
            return Forbid();

        var part = await repo.GetAsync(id);

        if (part == null)
            return NotFound();

        if (part.SellerId != sellerId)
            return Forbid();

        return View(part);
    }

    // DELETE - POST
    [Authorize(Roles = "Seller")]
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(
        string id)
    {
        var sellerId = GetSellerId();

        if (sellerId == null)
            return Forbid();

        var deleted = await repo.DeleteForSellerAsync(
            id,
            sellerId);

        if (!deleted)
            return NotFound();

        return RedirectToAction(nameof(Manage));
    }

    // CURRENT AUTHENTICATED SELLER
    private string? GetSellerId()
    {
        return User.FindFirstValue(
            ClaimTypes.NameIdentifier);
    }

    // VALIDATE VEHICLE AGAINST NHTSA
    private async Task<bool> ValidateVehicleAsync(
        CarPart part)
    {
        if (part.VehicleYear == null &&
            part.VehicleMakeId == null &&
            part.VehicleModelId == null)
        {
            part.VehicleMakeName = "";
            part.VehicleModelName = "";
            return true;
        }

        if (part.VehicleYear == null ||
            part.VehicleMakeId == null ||
            part.VehicleModelId == null)
        {
            ModelState.AddModelError(
                "",
                "Select a vehicle year, make, and model.");

            return false;
        }

        if (part.VehicleYear < 1996 ||
            part.VehicleYear > DateTime.UtcNow.Year + 2 ||
            part.VehicleMakeId <= 0 ||
            part.VehicleModelId <= 0)
        {
            ModelState.AddModelError(
                "",
                "Invalid vehicle selection.");

            return false;
        }

        try
        {
            var makes = await vehicleApi.GetMakesAsync();

            var make = makes.FirstOrDefault(
                item => item.MakeId == part.VehicleMakeId);

            if (make == null)
            {
                ModelState.AddModelError(
                    "",
                    "The selected manufacturer was not found.");

                return false;
            }

            var models = await vehicleApi.GetModelsAsync(
                make.MakeId,
                part.VehicleYear.Value);

            var model = models.FirstOrDefault(
                item => item.ModelId == part.VehicleModelId);

            if (model == null)
            {
                ModelState.AddModelError(
                    "",
                    "The selected model is not valid for this vehicle.");

                return false;
            }

            part.VehicleMakeName = make.MakeName;
            part.VehicleModelName = model.ModelName;

            return true;
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(
                "",
                "Vehicle service unavailable. Please try again.");

            return false;
        }
        catch (TaskCanceledException)
        {
            ModelState.AddModelError(
                "",
                "Vehicle service timed out. Please try again.");

            return false;
        }
    }
}