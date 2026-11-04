
using DriveHub.Models;
using DriveHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DriveHub.Controllers;

public class PartsController(PartRepository repo) : Controller
{
    // PUBLIC MARKETPLACE
    // Anyone can browse available parts.
    public async Task<IActionResult> Index(
        string? search,
        PartType? category)
    {
        var parts = await repo.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            parts = parts.Where(p =>
                p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Manufacturer.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.PartNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        if (category.HasValue)
        {
            parts = parts.Where(p =>
                p.Category == category.Value
            ).ToList();
        }

        ViewBag.Search = search;
        ViewBag.Category = category;

        return View(parts);
    }

    // PUBLIC PART DETAILS
    public async Task<IActionResult> Details(string id)
    {
        var part = await repo.GetAsync(id);

        if (part == null)
            return NotFound();

        return View(part);
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

        // Only retrieve listings owned by this seller.
        var allParts = await repo.GetBySellerAsync(sellerId);

        // Dashboard statistics
        ViewBag.TotalParts = allParts.Count;

        ViewBag.InventoryValue = allParts.Sum(
            p => p.Price * p.Quantity
        );

        ViewBag.LowStock = allParts.Count(
            p => p.Quantity > 0 && p.Quantity <= 5
        );

        ViewBag.OutOfStock = allParts.Count(
            p => p.Quantity == 0
        );

        var parts = allParts.AsEnumerable();

        // Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            parts = parts.Where(p =>
                p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Manufacturer.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.PartNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
            );
        }

        // Category filter
        if (category.HasValue)
        {
            parts = parts.Where(p =>
                p.Category == category.Value
            );
        }

        // Stock filters
        if (stock == "low")
        {
            parts = parts.Where(p =>
                p.Quantity > 0 && p.Quantity <= 5
            );
        }
        else if (stock == "out")
        {
            parts = parts.Where(p =>
                p.Quantity == 0
            );
        }
        else if (stock == "available")
        {
            parts = parts.Where(p =>
                p.Quantity > 0
            );
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
        [Bind("Name,Manufacturer,PartNumber,Category,Condition,Grade,QualityScore,Price,Quantity,WarrantyMonths,MileageKm,Description")]
        CarPart part)
    {
        var sellerId = GetSellerId();

        if (sellerId == null)
            return Forbid();

        if (!ModelState.IsValid)
            return View(part);

        // Seller ownership comes from the authenticated user,
        // not from the submitted form.
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
        [Bind("Name,Manufacturer,PartNumber,Category,Condition,Grade,QualityScore,Price,Quantity,WarrantyMonths,MileageKm,Description")]
        CarPart part)
    {
        var sellerId = GetSellerId();

        if (sellerId == null)
            return Forbid();

        if (!ModelState.IsValid)
        {
            part.Id = id;
            return View(part);
        }

        // Never accept a document ID or owner ID from form data.
        part.Id = id;

        var updated = await repo.UpdateForSellerAsync(
            part,
            sellerId
        );

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
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var sellerId = GetSellerId();

        if (sellerId == null)
            return Forbid();

        var deleted = await repo.DeleteForSellerAsync(
            id,
            sellerId
        );

        if (!deleted)
            return NotFound();

        return RedirectToAction(nameof(Manage));
    }

    // Retrieve the verified Firebase UID from the login cookie.
    private string? GetSellerId()
    {
        return User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );
    }
}

