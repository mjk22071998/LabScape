using CommunityToolkit.Mvvm.ComponentModel;
using LayoutDesigner.Core.Models;

namespace LayoutDesigner.ViewModels.Items;

public partial class FurnitureItemViewModel : ItemViewModel
{
    public FurnitureItem FurnitureModel => (FurnitureItem)Model;

    [ObservableProperty]
    private string _furnitureType;

    [ObservableProperty]
    private string _fill;

    [ObservableProperty]
    private string _stroke;

    public override bool ShowCanvasLabel => false;

    public FurnitureItemViewModel(FurnitureItem model) : base(model)
    {
        _furnitureType = model.FurnitureType;
        _fill = model.Fill;
        _stroke = model.Stroke;
    }

    partial void OnFurnitureTypeChanged(string value) => FurnitureModel.FurnitureType = value;
    partial void OnFillChanged(string value) => FurnitureModel.Fill = value;
    partial void OnStrokeChanged(string value) => FurnitureModel.Stroke = value;
}
