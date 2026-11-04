
using DriveHub.Models;
using Google.Cloud.Firestore;

namespace DriveHub.Services;

public class PartRepository(FirestoreDb db)
{
    private CollectionReference Parts => db.Collection("parts");

    private static CarPart FromSnapshot(DocumentSnapshot s)
    {
        var d = s.ToDictionary();

        T Get<T>(string key, T fallback = default!) =>
            d.TryGetValue(key, out var v) && v is not null
                ? (T)Convert.ChangeType(v, typeof(T))
                : fallback;

        return new CarPart
        {
            Id = s.Id,
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
            MileageKm = d.TryGetValue("mileageKm", out var mileage)
                && mileage is not null
                    ? Convert.ToInt32(mileage)
                    : null,
            Description = Get("description", ""),
            SellerId = Get("sellerId", "")
        };
    }

    private static Dictionary<string, object?> ToData(CarPart p) => new()
    {
        ["name"] = p.Name,
        ["manufacturer"] = p.Manufacturer,
        ["partNumber"] = p.PartNumber,
        ["category"] = (int)p.Category,
        ["condition"] = (int)p.Condition,
        ["grade"] = (int)p.Grade,
        ["qualityScore"] = p.QualityScore,
        ["priceCents"] = decimal.ToInt64(p.Price * 100m),
        ["quantity"] = p.Quantity,
        ["warrantyMonths"] = p.WarrantyMonths,
        ["mileageKm"] = p.MileageKm,
        ["description"] = p.Description,
        ["sellerId"] = p.SellerId
    };

    // Public marketplace: all listings
    public async Task<List<CarPart>> GetAllAsync()
    {
        var snapshot = await Parts.GetSnapshotAsync();

        return snapshot.Documents
            .Select(FromSnapshot)
            .OrderBy(p => p.Name)
            .ToList();
    }

    // Public listing details
    public async Task<CarPart?> GetAsync(string id)
    {
        var snapshot = await Parts.Document(id).GetSnapshotAsync();

        return snapshot.Exists ? FromSnapshot(snapshot) : null;
    }

    // Seller dashboard: only the seller's listings
    public async Task<List<CarPart>> GetBySellerAsync(string sellerId)
    {
        if (string.IsNullOrWhiteSpace(sellerId))
            throw new ArgumentException("Seller ID is required.");

        var snapshot = await Parts
            .WhereEqualTo("sellerId", sellerId)
            .GetSnapshotAsync();

        return snapshot.Documents
            .Select(FromSnapshot)
            .OrderBy(p => p.Name)
            .ToList();
    }

    // New listings receive ownership from the server
    public async Task AddForSellerAsync(CarPart part, string sellerId)
    {
        if (string.IsNullOrWhiteSpace(sellerId))
            throw new ArgumentException("Seller ID is required.");

        part.Id = "";
        part.SellerId = sellerId;

        await Parts.AddAsync(ToData(part));
    }

    // Atomically check ownership and update the listing
    public async Task<bool> UpdateForSellerAsync(
        CarPart part,
        string sellerId)
    {
        if (string.IsNullOrWhiteSpace(sellerId))
            return false;

        var doc = Parts.Document(part.Id);

        return await db.RunTransactionAsync(async transaction =>
        {
            var snapshot = await transaction.GetSnapshotAsync(doc);

            if (!snapshot.Exists)
                return false;

            var existing = FromSnapshot(snapshot);

            if (existing.SellerId != sellerId)
                return false;

            part.SellerId = existing.SellerId;

            transaction.Set(doc, ToData(part));

            return true;
        });
    }

    // Atomically check ownership and delete the listing
    public async Task<bool> DeleteForSellerAsync(
        string id,
        string sellerId)
    {
        if (string.IsNullOrWhiteSpace(sellerId))
            return false;

        var doc = Parts.Document(id);

        return await db.RunTransactionAsync(async transaction =>
        {
            var snapshot = await transaction.GetSnapshotAsync(doc);

            if (!snapshot.Exists)
                return false;

            var existing = FromSnapshot(snapshot);

            if (existing.SellerId != sellerId)
                return false;

            transaction.Delete(doc);

            return true;
        });
    }

    // Existing CRUD methods retained temporarily.
    // Remove these once PartsController uses the secured methods.

    public async Task AddAsync(CarPart part) =>
        await Parts.AddAsync(ToData(part));

    public async Task<bool> UpdateAsync(CarPart part)
    {
        var doc = Parts.Document(part.Id);

        if (!(await doc.GetSnapshotAsync()).Exists)
            return false;

        await doc.SetAsync(ToData(part));
        return true;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var doc = Parts.Document(id);

        if (!(await doc.GetSnapshotAsync()).Exists)
            return false;

        await doc.DeleteAsync();
        return true;
    }
}
