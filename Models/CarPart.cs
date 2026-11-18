
using System.ComponentModel.DataAnnotations;

namespace DriveHub.Models;

public enum PartType
{
    Brakes,
    Engine,
    Suspension,
    Transmission,
    Electrical,
    Exhaust,
    Body,
    Cooling,
    Tires
}

public enum PartCondition
{
    New,
    Used,
    Refurbished
}

public enum CarPartGrade
{
    NotGraded,
    A,
    B,
    C
}

public class CarPart
{
    public string Id { get; set; } = "";

    // Seller ownership
    public string SellerId { get; set; } = "";

    // Part information
    [Required]
    [StringLength(120)]
    public string Name { get; set; } = "";

    [Required]
    [StringLength(80)]
    public string Manufacturer { get; set; } = "";

    [Required]
    [StringLength(80)]
    public string PartNumber { get; set; } = "";

    public PartType Category { get; set; }

    public PartCondition Condition { get; set; }

    public CarPartGrade Grade { get; set; }

    [Range(0, 100)]
    public int QualityScore { get; set; } = 80;

    [Range(typeof(decimal), "0.01", "1000000")]
    public decimal Price { get; set; }

    [Range(0, 100000)]
    public int Quantity { get; set; }

    [Range(0, 120)]
    public int WarrantyMonths { get; set; }

    [Range(0, 2000000)]
    public int? MileageKm { get; set; }

    [StringLength(1000)]
    public string Description { get; set; } = "";

    // Compatible vehicle information
    // Optional to preserve older listings.
    [Range(1996, 2100)]
    public int? VehicleYear { get; set; }

    public int? VehicleMakeId { get; set; }

    [StringLength(100)]
    public string VehicleMakeName { get; set; } = "";

    public int? VehicleModelId { get; set; }

    [StringLength(100)]
    public string VehicleModelName { get; set; } = "";
}

