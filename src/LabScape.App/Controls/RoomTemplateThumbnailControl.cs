using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LabScape.App.Helpers;
using LabScape.ViewModels.Templates;

namespace LabScape.App.Controls;

public class RoomTemplateThumbnailControl : ContentControl
{
    public static readonly DependencyProperty RoomTemplateProperty =
        DependencyProperty.Register(
            nameof(RoomTemplate),
            typeof(SavedRoomTemplate),
            typeof(RoomTemplateThumbnailControl),
            new PropertyMetadata(null, (d, e) =>
            {
                if (d is RoomTemplateThumbnailControl ctrl)
                {
                    ctrl.OnRoomTemplateChanged(e.OldValue as SavedRoomTemplate, e.NewValue as SavedRoomTemplate);
                }
            }));

    public static readonly DependencyProperty OptionsProperty =
        DependencyProperty.Register(
            nameof(Options),
            typeof(RoomTemplateOptions),
            typeof(RoomTemplateThumbnailControl),
            new PropertyMetadata(null, (d, e) =>
            {
                if (d is RoomTemplateThumbnailControl ctrl)
                {
                    ctrl.RebuildVisual();
                }
            }));

    public SavedRoomTemplate? RoomTemplate
    {
        get => (SavedRoomTemplate?)GetValue(RoomTemplateProperty);
        set => SetValue(RoomTemplateProperty, value);
    }

    public RoomTemplateOptions? Options
    {
        get => (RoomTemplateOptions?)GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }

    private readonly Canvas _canvas;
    private readonly Viewbox _viewbox;

    public RoomTemplateThumbnailControl()
    {
        Focusable = false;
        IsHitTestVisible = false; // Mouse events cleanly pass through to parent cards for drag & drop!

        _canvas = new Canvas { Background = Brushes.Transparent };
        _viewbox = new Viewbox
        {
            Stretch = Stretch.Uniform,
            Margin = new Thickness(4),
            Child = _canvas
        };

        Content = _viewbox;
        Loaded += (s, e) => RebuildVisual();
    }

    private void OnRoomTemplateChanged(SavedRoomTemplate? oldTemplate, SavedRoomTemplate? newTemplate)
    {
        if (oldTemplate != null)
        {
            oldTemplate.PropertyChanged -= Template_PropertyChanged;
        }
        if (newTemplate != null)
        {
            newTemplate.PropertyChanged += Template_PropertyChanged;
        }
        RebuildVisual();
    }

    private void Template_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        RebuildVisual();
    }

    public void RebuildVisual()
    {
        var opts = RoomTemplate?.Options ?? Options;
        if (opts == null)
        {
            _canvas.Children.Clear();
            return;
        }

        var result = RoomTemplateGenerator.Generate(opts);
        RoomTemplateRenderHelper.PopulateCanvas(_canvas, result, this);
    }
}
