using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LayoutDesigner.ViewModels.Stencils;

namespace LayoutDesigner.ViewModels;

public partial class StencilsViewModel : ObservableObject
{
    private readonly List<StencilItem> _allStencils;

    [ObservableProperty]
    private string _searchFilter = string.Empty;

    [ObservableProperty]
    private StencilItem? _selectedStencil;

    public ObservableCollection<StencilItem> FilteredDevices { get; } = new();
    public ObservableCollection<StencilItem> FilteredFurniture { get; } = new();
    public ObservableCollection<StencilItem> FilteredShapes { get; } = new();

    public StencilsViewModel()
    {
        _allStencils = StencilItem.GetAllStencils();
        ApplyFilter();
    }

    partial void OnSearchFilterChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        FilteredDevices.Clear();
        FilteredFurniture.Clear();
        FilteredShapes.Clear();

        var query = SearchFilter?.Trim().ToLowerInvariant() ?? string.Empty;
        var filtered = string.IsNullOrEmpty(query)
            ? _allStencils
            : _allStencils.Where(s => s.Title.ToLowerInvariant().Contains(query) || s.Category.ToLowerInvariant().Contains(query));

        foreach (var s in filtered)
        {
            switch (s.Category)
            {
                case "Devices":
                    FilteredDevices.Add(s);
                    break;
                case "Furniture":
                    FilteredFurniture.Add(s);
                    break;
                case "Shapes":
                    FilteredShapes.Add(s);
                    break;
            }
        }
    }
}
