
using DriveHub.Models;
using Google.Cloud.Firestore;

namespace DriveHub.Services;

public class PartRepository(FirestoreDb db)
{
    private CollectionReference Parts => db.Collection("parts");

    // Convert a Firestore document into a CarPart.
    private static CarPart FromSnapshot(DocumentSnapshot snapshot)
    {
        var data = snapshot.ToDictionary();

        T Get<T>(string key, T fallback = default!)
        {
            if (!data.TryGetValue(key, out var value) ||
                value is null)
            {
                return fallback;
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }

        int? GetNullableInt(string key)
        {
            if (!data.TryGetValue(key, out var value) ||
                value is null)
            {
                return null;
            }

            return Convert.ToInt32(value);
        }

        return new CarPart
        {
            Id = snapshot.Id,
            SellerId = Get("sellerId", ""),

            Name = Get("name", ""),
            Manufacturer = Get("manufacturer", ""),
            PartNumber = Get("partNumber", ""),

            Category = (PartType)Get("category", 0),
            Condition = (PartCondition)Get("condition", 0),
            Grade = (CarPartGrade)Get("grade", 0),

            QualityScore = Get("qualityScore", 80),
            Price = Get("priceCents", 0L) / 100m,
            Quantity = Get("quantity", 0),
            WarrantyMonths = Get("warrantyMonths", 0),
            MileageKm = GetNullableInt("mileageKm"),
            Description = Get("description", ""),

            VehicleYear = GetNullableInt("vehicleYear"),
            VehicleMakeId = GetNullableInt("vehicleMakeId"),
            VehicleMakeName = Get("vehicleMakeName", ""),
            VehicleModelId = GetNullableInt("vehicleModelId"),
            VehicleModelName = Get("vehicleModelName", "")
        };
    }

    // Convert a CarPart into Firestore fields.
    private static Dictionary<string, object?> ToData(CarPart part)
    {
        return new Dictionary<string, object?>
        {
            ["sellerId"] = part.SellerId,

            ["name"] = part.Name,
            ["manufacturer"] = part.Manufacturer,
            ["partNumber"] = part.PartNumber,

            ["category"] = (int)part.Category,
            ["condition"] = (int)part.Condition,
            ["grade"] = (int)part.Grade,

            ["qualityScore"] = part.QualityScore,
            ["priceCents"] = decimal.ToInt64(part.Price * 100m),
            ["quantity"] = part.Quantity,
            ["warrantyMonths"] = part.WarrantyMonths,
            ["mileageKm"] = part.MileageKm,
            ["description"] = part.Description,

            ["vehicleYear"] = part.VehicleYear,
            ["vehicleMakeId"] = part.VehicleMakeId,
            ["vehicleMakeName"] = part.VehicleMakeName,
            ["vehicleModelId"] = part.VehicleModelId,
            ["vehicleModelName"] = part.VehicleModelName
        };
    }

    // PUBLIC MARKETPLACE
    public async Task<List<CarPart>> GetAllAsync()
    {
        var snapshot = await Parts.GetSnapshotAsync();

        return snapshot.Documents
            .Select(FromSnapshot)
            .OrderBy(part => part.Name)
            .ToList();
    }

    // PUBLIC PART DETAILS
    public async Task<CarPart?> GetAsync(string id)
    {
        var snapshot = await Parts.Document(id)
            .GetSnapshotAsync();

        return snapshot.Exists
            ? FromSnapshot(snapshot)
            : null;
    }

    // SELLER DASHBOARD
    public async Task<List<CarPart>> GetBySellerAsync(
        string sellerId)
    {
        if (string.IsNullOrWhiteSpace(sellerId))
            throw new ArgumentException("Seller ID is required.");

        var snapshot = await Parts
            .WhereEqualTo("sellerId", sellerId)
            .GetSnapshotAsync();

        return snapshot.Documents
            .Select(FromSnapshot)
            .OrderBy(part => part.Name)
            .ToList();
    }

    // CREATE SELLER-OWNED PART
    public async Task AddForSellerAsync(
        CarPart part,
        string sellerId)
    {
        if (string.IsNullOrWhiteSpace(sellerId))
            throw new ArgumentException("Seller ID is required.");

        part.Id = "";
        part.SellerId = sellerId;

        await Parts.AddAsync(ToData(part));
    }

    // UPDATE ONLY IF THE SELLER OWNS THE PART
    public async Task<bool> UpdateForSellerAsync(
        CarPart part,
        string sellerId)
    {
        if (string.IsNullOrWhiteSpace(sellerId))
            return false;

        var document = Parts.Document(part.Id);

        return await db.RunTransactionAsync(
            async transaction =>
            {
                var snapshot =
                    await transaction.GetSnapshotAsync(document);

                if (!snapshot.Exists)
                    return false;

                var existing = FromSnapshot(snapshot);

                if (existing.SellerId != sellerId)
                    return false;

                part.SellerId = existing.SellerId;

                transaction.Set(document, ToData(part));

                return true;
            });
    }

    // DELETE ONLY IF THE SELLER OWNS THE PART
    public async Task<bool> DeleteForSellerAsync(
        string id,
        string sellerId)
    {
        if (string.IsNullOrWhiteSpace(sellerId))
            return false;

        var document = Parts.Document(id);

        return await db.RunTransactionAsync(
            async transaction =>
            {
                var snapshot =
                    await transaction.GetSnapshotAsync(document);

                if (!snapshot.Exists)
                    return false;

                var existing = FromSnapshot(snapshot);

                if (existing.SellerId != sellerId)
                    return false;

                transaction.Delete(document);

                return true;
            });
    }
}

