using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using LabScape.Core.Enums;
using LabScape.Core.Interfaces;
using LabScape.Core.Models;
using LabScape.ViewModels.Items;

namespace LabScape.ViewModels;

public partial class LayoutTabViewModel : ObservableObject
{
    private readonly IUndoRedoService _undoRedoService;

    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _title = "Untitled Floorplan";

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private bool _isModified;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private int _validationWarningCount;

    [ObservableProperty]
    private int _validationErrorCount;

    public CanvasViewModel Canvas { get; }
    public IUndoRedoService UndoRedoService => _undoRedoService;

    public LayoutTabViewModel(string title, IUndoRedoService undoRedoService, string filePath = "")
    {
        _title = title;
        _filePath = filePath;
        _undoRedoService = undoRedoService;

        Canvas = new CanvasViewModel(_undoRedoService);

        _undoRedoService.StateChanged += (s, e) =>
        {
            IsModified = true;
            UpdateValidation();
        };

        Canvas.Items.CollectionChanged += CanvasItems_CollectionChanged;
        UpdateValidation();
    }

    private void CanvasItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        IsModified = true;
        UpdateValidation();
    }

    public void UpdateValidation()
    {
        Canvas.RecalculateValidation();
        var allDevices = GetAllDeviceItems(Canvas.Items).ToList();
        ValidationWarningCount = allDevices.Count(d => d.ValidationStatus == ValidationStatus.Unassigned);
        ValidationErrorCount = allDevices.Count(d => d.ValidationStatus == ValidationStatus.DuplicateIp);
    }

    private IEnumerable<DeviceItemViewModel> GetAllDeviceItems(IEnumerable<ItemViewModel> items)
    {
        foreach (var itm in items)
        {
            if (itm is DeviceItemViewModel dev)
                yield return dev;
            else if (itm is GroupItemViewModel grp)
            {
                foreach (var childDev in GetAllDeviceItems(grp.Children))
                    yield return childDev;
            }
        }
    }

    public LayoutDocument BuildDocumentModel()
    {
        var doc = new LayoutDocument
        {
            Metadata = new DocumentMetadata
            {
                Title = Title,
                CanvasWidth = Canvas.CanvasWidth,
                CanvasHeight = Canvas.CanvasHeight,
                GridSize = Canvas.GridSize
            }
        };

        foreach (var itemVm in Canvas.Items)
        {
            doc.Items.Add(itemVm.Model);
        }

        return doc;
    }

    public void LoadDocumentModel(LayoutDocument doc)
    {
        Canvas.Items.Clear();
        Canvas.ClearSelection();
        _undoRedoService.Clear();

        Canvas.CanvasWidth = doc.Metadata.CanvasWidth;
        Canvas.CanvasHeight = doc.Metadata.CanvasHeight;
        Canvas.GridSize = doc.Metadata.GridSize;
        if (!string.IsNullOrWhiteSpace(doc.Metadata.Title))
        {
            Title = doc.Metadata.Title;
        }

        foreach (var itemModel in doc.Items)
        {
            Canvas.Items.Add(ItemViewModel.FromModel(itemModel));
        }

        UpdateValidation();
        IsModified = false;
    }
}
