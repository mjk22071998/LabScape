using System.Text.Json.Serialization;
using LayoutDesigner.Core.Enums;

namespace LayoutDesigner.Core.Models;

public class GroupItem : LayoutItemBase
{
    [JsonIgnore]
    public override ItemType Type => ItemType.Group;

    [JsonPropertyName("children")]
    public List<LayoutItemBase> Children { get; set; } = new();

    public GroupItem()
    {
        Name = "New Group";
    }

    /// <summary>
    /// Recalculates bounding box (X, Y, Width, Height) from children items
    /// </summary>
    public void RecalculateBounds()
    {
        if (Children.Count == 0) return;

        double minX = Children.Min(c => c.X);
        double minY = Children.Min(c => c.Y);
        double maxX = Children.Max(c => c.X + c.Width);
        double maxY = Children.Max(c => c.Y + c.Height);

        X = minX;
        Y = minY;
        Width = Math.Max(10, maxX - minX);
        Height = Math.Max(10, maxY - minY);
    }
}
