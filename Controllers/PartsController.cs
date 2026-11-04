using DriveHub.Models;
using DriveHub.Services;
using Microsoft.AspNetCore.Mvc;
namespace DriveHub.Controllers;
public class PartsController(PartRepository repo) : Controller {
 public async Task<IActionResult> Index(string? search, PartType? category) {
  var parts=await repo.GetAllAsync();
  if(!string.IsNullOrWhiteSpace(search)) parts=parts.Where(p=>p.Name.Contains(search,StringComparison.OrdinalIgnoreCase)||p.Manufacturer.Contains(search,StringComparison.OrdinalIgnoreCase)||p.PartNumber.Contains(search,StringComparison.OrdinalIgnoreCase)).ToList();
  if(category.HasValue) parts=parts.Where(p=>p.Category==category.Value).ToList();
  ViewBag.Search=search; ViewBag.Category=category; return View(parts);
 }
 public async Task<IActionResult> Details(string id) { var p=await repo.GetAsync(id); return p==null?NotFound():View(p); }
public async Task<IActionResult> Manage(string? search, PartType? category, string? stock)
{
    var allParts = await repo.GetAllAsync();

    ViewBag.TotalParts = allParts.Count;
    ViewBag.InventoryValue = allParts.Sum(p => p.Price * p.Quantity);
    ViewBag.LowStock = allParts.Count(p => p.Quantity > 0 && p.Quantity <= 5);
    ViewBag.OutOfStock = allParts.Count(p => p.Quantity == 0);

    var parts = allParts.AsEnumerable();

    if (!string.IsNullOrWhiteSpace(search))
    {
        parts = parts.Where(p =>
            p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            (p.Manufacturer?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (p.PartNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
    }

    if (category.HasValue)
        parts = parts.Where(p => p.Category == category.Value);

    if (stock == "low")
        parts = parts.Where(p => p.Quantity > 0 && p.Quantity <= 5);
    else if (stock == "out")
        parts = parts.Where(p => p.Quantity == 0);
    else if (stock == "available")
        parts = parts.Where(p => p.Quantity > 0);

    ViewBag.Search = search;
    ViewBag.Category = category;
    ViewBag.Stock = stock;

    return View(parts.ToList());
}
 public IActionResult Create() => View(new CarPart());
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Create(CarPart part) {
  if(!ModelState.IsValid)return View(part); await repo.AddAsync(part); return RedirectToAction(nameof(Manage));
 }
 public async Task<IActionResult> Edit(string id) { var p=await repo.GetAsync(id); return p==null?NotFound():View(p); }
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Edit(string id,CarPart part) {
  if(id!=part.Id)return BadRequest(); if(!ModelState.IsValid)return View(part);
  if(!await repo.UpdateAsync(part))return NotFound(); return RedirectToAction(nameof(Manage));
 }
 public async Task<IActionResult> Delete(string id) { var p=await repo.GetAsync(id); return p==null?NotFound():View(p); }
 [HttpPost,ActionName("Delete"),ValidateAntiForgeryToken] public async Task<IActionResult> DeleteConfirmed(string id) {
  if(!await repo.DeleteAsync(id))return NotFound(); return RedirectToAction(nameof(Manage));
 }
}
