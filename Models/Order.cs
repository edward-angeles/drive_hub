
using System;
using System.ComponentModel.DataAnnotations;

namespace DriveHub.Models;

// Which payment system was used?
public enum PaymentProvider
{
    Stripe,
    Simulation
}

// Current payment status
public enum PaymentStatus
{
    Pending,
    Paid,
    Failed,
    Cancelled,
    Refunded
}

// Which checkout environment was used?
public enum CheckoutMode
{
    StripeTest,
    StripeLive,
    Simulation
}

public class Order
{
    // Firestore document ID
    public string Id { get; set; } = "";

    // Customer-facing order number
    public string OrderNumber { get; set; } = "";

    // Firebase UID of the buyer
    [Required]
    public string BuyerId { get; set; } = "";

    // Shipping address snapshot
    // Stored at checkout so later profile
    // changes do not affect old orders.
    [Required]
    public string ShippingStreet { get; set; } = "";

    [Required]
    public string ShippingCity { get; set; } = "";

    [Required]
    public string ShippingProvince { get; set; } = "";

    [Required]
    public string ShippingPostalCode { get; set; } = "";

    // Monetary amounts stored in cents
    // to avoid floating-point errors.
    [Range(0, long.MaxValue)]
    public long MerchandiseTotalCents { get; set; }

    // Currency used for the order
    public string Currency { get; set; } = "cad";

    // Payment information
    public PaymentProvider PaymentProvider { get; set; }
        = PaymentProvider.Stripe;

    public PaymentStatus PaymentStatus { get; set; }
        = PaymentStatus.Pending;

    public CheckoutMode CheckoutMode { get; set; }
        = CheckoutMode.StripeTest;

    // Stripe identifiers
    public string? StripeCheckoutSessionId { get; set; }

    public string? StripePaymentIntentId { get; set; }

    // Order timestamps (UTC)
    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;

    public DateTime? PaidAt { get; set; }
}
