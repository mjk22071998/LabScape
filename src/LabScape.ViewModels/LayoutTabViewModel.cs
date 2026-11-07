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
        Canvas.ValidationRecalculated += (s, e) => SyncValidationCounts();
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
        SyncValidationCounts();
    }

    private void SyncValidationCounts()
    {
        var allDevices = GetAllDeviceItems(Canvas.Items).ToList();
        ValidationWarningCount = allDevices.Count(d => d.ValidationStatus == ValidationStatus.Unassigned);
        ValidationErrorCount = allDevices.Count(d => d.ValidationStatus == ValidationStatus.DuplicateIp || d.ValidationStatus == ValidationStatus.InvalidIpFormat);
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
                PagePreset = Canvas.PageSize.ToString(),
                PageOrientation = Canvas.PageOrientation.ToString(),
                AutoSize = Canvas.AutoSize,
                BasePageWidth = Canvas.BasePageWidth,
                BasePageHeight = Canvas.BasePageHeight,
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

        Canvas.AutoSize = doc.Metadata.AutoSize;
        Canvas.BasePageWidth = doc.Metadata.BasePageWidth > 0 ? doc.Metadata.BasePageWidth : 1587;
        Canvas.BasePageHeight = doc.Metadata.BasePageHeight > 0 ? doc.Metadata.BasePageHeight : 1123;
        Canvas.CanvasWidth = doc.Metadata.CanvasWidth;
        Canvas.CanvasHeight = doc.Metadata.CanvasHeight;
        Canvas.GridSize = doc.Metadata.GridSize;

        if (Enum.TryParse<PageSizePreset>(doc.Metadata.PagePreset, out var preset))
        {
            Canvas.PageSize = preset;
        }
        else
        {
            Canvas.PageSize = PageDimensions.DetectPreset(doc.Metadata.CanvasWidth, doc.Metadata.CanvasHeight, out _);
        }

        if (Enum.TryParse<PageOrientation>(doc.Metadata.PageOrientation, out var orient))
        {
            Canvas.PageOrientation = orient;
        }
        else
        {
            Canvas.PageOrientation = doc.Metadata.CanvasWidth >= doc.Metadata.CanvasHeight ? PageOrientation.Landscape : PageOrientation.Portrait;
        }
        if (!string.IsNullOrWhiteSpace(doc.Metadata.Title))
        {
            Title = doc.Metadata.Title;
        }

        foreach (var itemModel in doc.Items)
        {
            Canvas.Items.Add(ItemViewModel.FromModel(itemModel));
        }

        Canvas.UpdatePageGrid();
        UpdateValidation();
        IsModified = false;
    }
}
