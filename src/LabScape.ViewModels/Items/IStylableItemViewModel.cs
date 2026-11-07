namespace LabScape.ViewModels.Items;

/// <summary>
/// Defines an item ViewModel that supports customizable appearance (Fill, Stroke, and StrokeThickness).
/// </summary>
public interface IStylableItemViewModel
{
    string? Fill { get; set; }
    string? Stroke { get; set; }
    double StrokeThickness { get; set; }
}
