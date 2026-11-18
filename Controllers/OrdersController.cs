
using DriveHub.Models;
using DriveHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DriveHub.Controllers;

[Authorize]
public class OrdersController(
    OrderRepository repo) : Controller
{
    // Get the logged-in user's Firebase UID.
    private string? GetUserId()
    {
        return User.FindFirstValue(
            ClaimTypes.NameIdentifier);
    }

    // BUYER ORDER HISTORY
    // GET: /Orders
    // GET: /Orders/Index
    public async Task<IActionResult> Index()
    {
        var buyerId = GetUserId();

        if (string.IsNullOrWhiteSpace(buyerId))
            return Challenge();

        var orders = await repo.GetByBuyerAsync(buyerId);

        return View(orders);
    }

    // BUYER ORDER DETAILS
    // GET: /Orders/Details/{id}
    public async Task<IActionResult> Details(string id)
    {
        var buyerId = GetUserId();

        if (string.IsNullOrWhiteSpace(buyerId))
            return Challenge();

        if (string.IsNullOrWhiteSpace(id))
            return NotFound();

        var order = await repo.GetForBuyerAsync(
            id, buyerId);

        if (order == null)
            return NotFound();

        var items = await repo.GetItemsForBuyerAsync(
            id, buyerId);

        ViewBag.OrderItems = items;

        return View(order);
    }

    // SELLER ORDER DASHBOARD
    // GET: /Orders/Seller
    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> Seller()
    {
        var sellerId = GetUserId();

        if (string.IsNullOrWhiteSpace(sellerId))
            return Challenge();

        var items = await repo.GetBySellerAsync(
            sellerId);

        return View(items);
    }

    // SELLER UPDATES AN ORDER ITEM
    // POST: /Orders/UpdateFulfillment
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> UpdateFulfillment(
        string itemId,
        FulfillmentStatus newStatus)
    {
        var sellerId = GetUserId();

        if (string.IsNullOrWhiteSpace(sellerId))
            return Challenge();

        if (string.IsNullOrWhiteSpace(itemId))
            return BadRequest("Order item ID is required.");

        if (!Enum.IsDefined(newStatus))
            return BadRequest("Invalid fulfillment status.");

        var updated = await repo.UpdateFulfillmentAsync(
            itemId,
            sellerId,
            newStatus);

        if (!updated)
        {
            TempData["Error"] =
                "Unable to update this item. " +
                "Check its payment status, ownership, " +
                "or current fulfillment status.";
        }
        else
        {
            TempData["Success"] =
                "Fulfillment status updated successfully.";
        }

        return RedirectToAction(nameof(Seller));
    }
}
