using CommunityToolkit.Mvvm.ComponentModel;
using LabScape.Core.Models;

namespace LabScape.ViewModels.Items;

public partial class FurnitureItemViewModel : ItemViewModel, IStylableItemViewModel
{
    public FurnitureItem FurnitureModel => (FurnitureItem)Model;

    [ObservableProperty]
    private string _furnitureType;

    [ObservableProperty]
    private string? _fill;

    [ObservableProperty]
    private string? _stroke;

    [ObservableProperty]
    private double _strokeThickness = 2.0;

    public override bool ShowCanvasLabel => false;

    public FurnitureItemViewModel(FurnitureItem model) : base(model)
    {
        _furnitureType = model.FurnitureType;
        _fill = model.Fill;
        _stroke = model.Stroke;
        _strokeThickness = model.StrokeThickness ?? 2.0;
    }

    partial void OnFurnitureTypeChanged(string value) => FurnitureModel.FurnitureType = value;
    partial void OnFillChanged(string? value) => FurnitureModel.Fill = value ?? "#F3F4F6";
    partial void OnStrokeChanged(string? value) => FurnitureModel.Stroke = value ?? "#374151";
    partial void OnStrokeThicknessChanged(double value) => FurnitureModel.StrokeThickness = value;
}
