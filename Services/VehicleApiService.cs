
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace DriveHub.Services;

public class VehicleApiService
{
    private readonly HttpClient _client;

    public VehicleApiService(HttpClient client)
    {
        _client = client;
        _client.BaseAddress =
            new Uri("https://vpic.nhtsa.dot.gov/api/vehicles/");
        _client.Timeout = TimeSpan.FromSeconds(15);
    }

    // Load car manufacturers from NHTSA
    public async Task<List<VehicleMake>> GetMakesAsync()
    {
        var response = await _client.GetFromJsonAsync<
            NhtsaResponse<VehicleMake>>(
                "GetMakesForVehicleType/car?format=json");

        return response?.Results?
            .Where(m => !string.IsNullOrWhiteSpace(m.MakeName))
            .GroupBy(m => m.MakeId)
            .Select(g => g.First())
            .OrderBy(m => m.MakeName)
            .ToList() ?? new();
    }

    // Load models for a manufacturer and year
    public async Task<List<VehicleModel>> GetModelsAsync(
        int makeId,
        int year)
    {
        var response = await _client.GetFromJsonAsync<
            NhtsaResponse<VehicleModel>>(
                $"GetModelsForMakeIdYear/makeId/{makeId}/modelyear/{year}?format=json");

        return response?.Results?
            .Where(m => !string.IsNullOrWhiteSpace(m.ModelName))
            .GroupBy(m => m.ModelId)
            .Select(g => g.First())
            .OrderBy(m => m.ModelName)
            .ToList() ?? new();
    }
}

public class NhtsaResponse<T>
{
    public List<T> Results { get; set; } = new();
}

public class VehicleMake
{
    [JsonPropertyName("MakeId")]
    public int MakeId { get; set; }

    [JsonPropertyName("MakeName")]
    public string MakeName { get; set; } = "";
}

public class VehicleModel
{
    [JsonPropertyName("Model_ID")]
    public int ModelId { get; set; }

    [JsonPropertyName("Model_Name")]
    public string ModelName { get; set; } = "";

    [JsonPropertyName("Make_ID")]
    public int MakeId { get; set; }

    [JsonPropertyName("Make_Name")]
    public string MakeName { get; set; } = "";
}
