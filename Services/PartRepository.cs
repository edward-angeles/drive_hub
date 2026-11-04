using DriveHub.Models;
using Google.Cloud.Firestore;

namespace DriveHub.Services;
public class PartRepository(FirestoreDb db) {
 private CollectionReference Parts => db.Collection("parts");
 private static CarPart FromSnapshot(DocumentSnapshot s) {
  var d = s.ToDictionary();
  T Get<T>(string key, T fallback = default!) => d.TryGetValue(key, out var v) && v is not null ? (T)Convert.ChangeType(v, typeof(T)) : fallback;
  return new CarPart {
   Id=s.Id, Name=Get("name", ""), Manufacturer=Get("manufacturer", ""), PartNumber=Get("partNumber", ""),
   Category=(PartType)Get("category", 0), Condition=(PartCondition)Get("condition", 0), Grade=(CarPartGrade)Get("grade", 0),
   QualityScore=Get("qualityScore", 80), Price=Get("priceCents", 0L)/100m,
   Quantity=Get("quantity", 0), WarrantyMonths=Get("warrantyMonths", 0),
   MileageKm=d.TryGetValue("mileageKm",out var mileage) && mileage is not null ? Convert.ToInt32(mileage) : null,
   Description=Get("description", "")
  };
 }
 private static Dictionary<string,object?> ToData(CarPart p) => new() {
  ["name"]=p.Name, ["manufacturer"]=p.Manufacturer, ["partNumber"]=p.PartNumber,
  ["category"]=(int)p.Category, ["condition"]=(int)p.Condition, ["grade"]=(int)p.Grade,
  ["qualityScore"]=p.QualityScore, ["priceCents"]=decimal.ToInt64(p.Price*100m),
  ["quantity"]=p.Quantity, ["warrantyMonths"]=p.WarrantyMonths, ["mileageKm"]=p.MileageKm,
  ["description"]=p.Description
 };
 public async Task<List<CarPart>> GetAllAsync() => (await Parts.GetSnapshotAsync()).Documents.Select(FromSnapshot).OrderBy(p=>p.Name).ToList();
 public async Task<CarPart?> GetAsync(string id) { var s=await Parts.Document(id).GetSnapshotAsync(); return s.Exists ? FromSnapshot(s) : null; }
 public async Task AddAsync(CarPart p) => await Parts.AddAsync(ToData(p));
 public async Task<bool> UpdateAsync(CarPart p) { var doc=Parts.Document(p.Id); if(!(await doc.GetSnapshotAsync()).Exists) return false; await doc.SetAsync(ToData(p)); return true; }
 public async Task<bool> DeleteAsync(string id) { var doc=Parts.Document(id); if(!(await doc.GetSnapshotAsync()).Exists) return false; await doc.DeleteAsync(); return true; }
}
