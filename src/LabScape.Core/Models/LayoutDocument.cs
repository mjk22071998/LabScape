using System.Text.Json.Serialization;

namespace LabScape.Core.Models;

public class DocumentMetadata
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "Untitled Floorplan";

    [JsonPropertyName("canvas_width")]
    public double CanvasWidth { get; set; } = 3000;

    [JsonPropertyName("canvas_height")]
    public double CanvasHeight { get; set; } = 2000;

    [JsonPropertyName("grid_size")]
    public double GridSize { get; set; } = 20.0;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("modified_at")]
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}

public class LayoutDocument
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("metadata")]
    public DocumentMetadata Metadata { get; set; } = new();

    [JsonPropertyName("items")]
    public List<LayoutItemBase> Items { get; set; } = new();
}
