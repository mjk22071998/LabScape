using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabScape.ViewModels.Commands;
using LabScape.ViewModels.Items;

namespace LabScape.ViewModels.Layers;

public partial class LayerItemViewModel : ObservableObject, IDisposable
{
    private readonly ItemViewModel _item;
    private readonly Action<ItemViewModel, string, string>? _onRename;
    private readonly Action<ItemViewModel, bool>? _onToggleLock;
    private readonly Action<ItemViewModel, bool>? _onToggleVisibility;

    public ItemViewModel Item => _item;

    public string Id => _item.Id;
    public string Name => _item.Name;
    public int Z => _item.Z;

    public bool IsSelected
    {
        get => _item.IsSelected;
        set
        {
            if (_item.IsSelected != value)
            {
                _item.IsSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsLocked
    {
        get => _item.IsLocked;
        set
        {
            if (_item.IsLocked != value)
            {
                _item.IsLocked = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsVisible
    {
        get => _item.IsVisible;
        set
        {
            if (_item.IsVisible != value)
            {
                _item.IsVisible = value;
                OnPropertyChanged();
            }
        }
    }

    public string ItemType => _item switch
    {
        DeviceItemViewModel => "Workstation",
        FurnitureItemViewModel => "Furniture",
        ShapeItemViewModel => "Shape",
        TextItemViewModel => "Text",
        GroupItemViewModel => "Group",
        _ => "Element"
    };

    public string IconKind => _item switch
    {
        DeviceItemViewModel => "Device",
        FurnitureItemViewModel => "Furniture",
        ShapeItemViewModel => "Shape",
        TextItemViewModel => "Text",
        GroupItemViewModel => "Group",
        _ => "Shape"
    };

    public string Subtitle => _item switch
    {
        DeviceItemViewModel d => !string.IsNullOrWhiteSpace(d.IpAddress) ? d.IpAddress : (d.DeviceId ?? "Unassigned IP"),
        ShapeItemViewModel s => $"{s.ShapeType} ({(int)s.Width}x{(int)s.Height})",
        FurnitureItemViewModel f => $"{f.FurnitureType} ({(int)f.Width}x{(int)f.Height})",
        TextItemViewModel t => string.IsNullOrWhiteSpace(t.Text) ? "Text Label" : t.Text,
        GroupItemViewModel g => $"{g.Children.Count} items grouped",
        _ => $"Z: {_item.Z}"
    };

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editText = string.Empty;

    public LayerItemViewModel(
        ItemViewModel item,
        Action<ItemViewModel, string, string>? onRename = null,
        Action<ItemViewModel, bool>? onToggleLock = null,
        Action<ItemViewModel, bool>? onToggleVisibility = null)
    {
        _item = item;
        _onRename = onRename;
        _onToggleLock = onToggleLock;
        _onToggleVisibility = onToggleVisibility;

        _item.PropertyChanged += Item_PropertyChanged;
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ItemViewModel.Name):
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(Subtitle));
                break;
            case nameof(ItemViewModel.Z):
                OnPropertyChanged(nameof(Z));
                OnPropertyChanged(nameof(Subtitle));
                break;
            case nameof(ItemViewModel.IsSelected):
                OnPropertyChanged(nameof(IsSelected));
                break;
            case nameof(ItemViewModel.IsLocked):
                OnPropertyChanged(nameof(IsLocked));
                break;
            case nameof(ItemViewModel.IsVisible):
                OnPropertyChanged(nameof(IsVisible));
                break;
            case nameof(ItemViewModel.Width):
            case nameof(ItemViewModel.Height):
                OnPropertyChanged(nameof(Subtitle));
                break;
        }
    }

    [RelayCommand]
    public void ToggleLock()
    {
        bool newLocked = !IsLocked;
        if (_onToggleLock != null)
        {
            _onToggleLock(_item, newLocked);
        }
        else
        {
            IsLocked = newLocked;
        }
    }

    [RelayCommand]
    public void ToggleVisibility()
    {
        bool newVisible = !IsVisible;
        if (_onToggleVisibility != null)
        {
            _onToggleVisibility(_item, newVisible);
        }
        else
        {
            IsVisible = newVisible;
        }
    }

    [RelayCommand]
    public void BeginEdit()
    {
        EditText = Name;
        IsEditing = true;
    }

    [RelayCommand]
    public void CommitEdit()
    {
        if (!IsEditing) return;
        string trimmed = EditText?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(trimmed) && trimmed != Name)
        {
            if (_onRename != null)
            {
                _onRename(_item, Name, trimmed);
            }
            else
            {
                _item.Name = trimmed;
            }
        }
        IsEditing = false;
    }

    [RelayCommand]
    public void CancelEdit()
    {
        IsEditing = false;
    }

    public void Dispose()
    {
        _item.PropertyChanged -= Item_PropertyChanged;
    }
}
