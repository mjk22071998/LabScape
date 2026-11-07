using System.Text.Json.Serialization;

namespace LabScape.Core.Models;

public class DocumentMetadata
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "Untitled Floorplan";

    [JsonPropertyName("canvas_width")]
    public double CanvasWidth { get; set; } = 1587;

    [JsonPropertyName("canvas_height")]
    public double CanvasHeight { get; set; } = 1123;

    [JsonPropertyName("page_preset")]
    public string PagePreset { get; set; } = "A3";

    [JsonPropertyName("page_orientation")]
    public string PageOrientation { get; set; } = "Landscape";

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
