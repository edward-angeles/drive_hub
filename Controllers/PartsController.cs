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
 public async Task<IActionResult> Manage() => View(await repo.GetAllAsync());
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
