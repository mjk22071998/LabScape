using System.Text.Json;
using System.Text.Json.Serialization;
using LabScape.Core.Models;
using LabScape.ViewModels.Items;

namespace LabScape.ViewModels.Services;

public static class LayoutClipboard
{
    private static readonly List<string> _serializedItemBuffer = new();
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    public static bool HasContent => _serializedItemBuffer.Count > 0;

    public static void Copy(IEnumerable<ItemViewModel> items)
    {
        _serializedItemBuffer.Clear();
        foreach (var item in items)
        {
            var json = JsonSerializer.Serialize(item.Model, SerializerOptions);
            _serializedItemBuffer.Add(json);
        }
    }

    public static List<ItemViewModel> Paste(double offsetX = 20.0, double offsetY = 20.0)
    {
        var result = new List<ItemViewModel>();
        foreach (var json in _serializedItemBuffer)
        {
            var model = JsonSerializer.Deserialize<LayoutItemBase>(json, SerializerOptions);
            if (model != null)
            {
                model.Id = Guid.NewGuid().ToString();
                model.X += offsetX;
                model.Y += offsetY;
                model.Z += 1;
                if (!string.IsNullOrEmpty(model.Name))
                {
                    model.Name = $"{model.Name} (Copy)";
                }

                result.Add(ItemViewModel.FromModel(model));
            }
        }
        return result;
    }

    public static List<ItemViewModel> Duplicate(IEnumerable<ItemViewModel> items, double offsetX = 20.0, double offsetY = 20.0)
    {
        var result = new List<ItemViewModel>();
        foreach (var item in items)
        {
            var json = JsonSerializer.Serialize(item.Model, SerializerOptions);
            var model = JsonSerializer.Deserialize<LayoutItemBase>(json, SerializerOptions);
            if (model != null)
            {
                model.Id = Guid.NewGuid().ToString();
                model.X += offsetX;
                model.Y += offsetY;
                model.Z += 1;
                if (!string.IsNullOrEmpty(model.Name))
                {
                    model.Name = $"{model.Name} (Copy)";
                }

                result.Add(ItemViewModel.FromModel(model));
            }
        }
        return result;
    }
}
