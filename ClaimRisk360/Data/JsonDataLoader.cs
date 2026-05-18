using System.Text.Json;

namespace ClaimRisk360.Data;

/// <summary>
/// Loads and deserializes JSON seed data files from Data/SeedData.
/// </summary>
public static class JsonDataLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static T Load<T>(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "SeedData", fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Seed data file not found: {path}");

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, Options)
               ?? throw new InvalidOperationException($"Failed to deserialize {fileName}");
    }
}
