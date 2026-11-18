
using System.ComponentModel.DataAnnotations;

namespace DriveHub.Models;


public enum FulfillmentStatus
{
    Pending,
    Accepted,
    Shipped,
    Completed,
    Cancelled
}

public class OrderItem
{
    // Firestore document ID
    public string Id { get; set; } = "";

    
    [Required]
    public string OrderId { get; set; } = "";

    
    [Required]
    public string PartId { get; set; } = "";

    // Firebase UID of the seller
    [Required]
    public string SellerId { get; set; } = "";

   
    [Required]
    [StringLength(120)]
    public string PartName { get; set; } = "";

    
    [Range(0, long.MaxValue)]
    public long UnitPriceCents { get; set; }

    
    [Range(1, 100000)]
    public int Quantity { get; set; } = 1;

    
    public FulfillmentStatus FulfillmentStatus { get; set; }
        = FulfillmentStatus.Pending;

    
    public long LineTotalCents =>
        UnitPriceCents * Quantity;
}
