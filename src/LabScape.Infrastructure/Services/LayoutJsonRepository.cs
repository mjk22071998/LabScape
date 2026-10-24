using System.Text.Json;
using System.Text.Json.Serialization;
using LabScape.Core.Interfaces;
using LabScape.Core.Models;

namespace LabScape.Infrastructure.Services;

public class LayoutJsonRepository : ILayoutRepository
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    public async Task<LayoutDocument> LoadFromFileAsync(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        var document = await JsonSerializer.DeserializeAsync<LayoutDocument>(stream, JsonOptions);
        return document ?? new LayoutDocument();
    }

    public async Task SaveToFileAsync(LayoutDocument document, string filePath)
    {
        document.Metadata.ModifiedAt = DateTime.UtcNow;
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, document, JsonOptions);
    }

    public string SerializeToJson(LayoutDocument document)
    {
        document.Metadata.ModifiedAt = DateTime.UtcNow;
        return JsonSerializer.Serialize(document, JsonOptions);
    }

    public LayoutDocument DeserializeFromJson(string json)
    {
        return JsonSerializer.Deserialize<LayoutDocument>(json, JsonOptions) ?? new LayoutDocument();
    }
}
