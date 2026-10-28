using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using LabScape.Core.Enums;

namespace LabScape.ViewModels.Templates;

public static class RoomTemplatePersistence
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string GetStorageDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LabScape");
    }

    public static string GetStorageFilePath()
    {
        return Path.Combine(GetStorageDirectory(), "templates.json");
    }

    public static List<SavedRoomTemplate> LoadTemplates(string? customPath = null)
    {
        string filePath = customPath ?? GetStorageFilePath();
        try
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                var list = JsonSerializer.Deserialize<List<SavedRoomTemplate>>(json, JsonOptions);
                if (list != null && list.Count > 0)
                {
                    return list;
                }
            }
        }
        catch
        {
            // Silently fallback to defaults on corrupt/incompatible files
        }

        var defaults = GetDefaultTemplates();
        SaveTemplates(defaults, filePath);
        return defaults;
    }

    public static void SaveTemplates(IEnumerable<SavedRoomTemplate> templates, string? customPath = null)
    {
        string filePath = customPath ?? GetStorageFilePath();
        try
        {
            string dir = Path.GetDirectoryName(filePath) ?? GetStorageDirectory();
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonSerializer.Serialize(templates, JsonOptions);
            File.WriteAllText(filePath, json);
        }
        catch
        {
            // Ignore write failures in sandboxed test or restricted runner environments
        }
    }

    public static List<SavedRoomTemplate> GetDefaultTemplates()
    {
        return new List<SavedRoomTemplate>
        {
            new()
            {
                Id = "preset_12_pc_full",
                Name = "12-PC Standard Lab",
                Description = "Full enclosed 4-wall perimeter with 12 modular workstations and ergonomic swivel chairs.",
                IsBuiltIn = true,
                Options = new RoomTemplateOptions
                {
                    Sides = 4,
                    PcCount = 12,
                    IncludeDesks = true,
                    IncludeChairs = true,
                    GroupElements = true
                }
            },
            new()
            {
                Id = "preset_24_pc_enterprise",
                Name = "24-PC Enterprise Lab",
                Description = "High-density 4-wall computer facility with 6 workstations distributed along each wall.",
                IsBuiltIn = true,
                Options = new RoomTemplateOptions
                {
                    Sides = 4,
                    PcCount = 24,
                    IncludeDesks = true,
                    IncludeChairs = true,
                    GroupElements = true
                }
            },
            new()
            {
                Id = "preset_8_pc_u_shape",
                Name = "8-PC U-Shape Studio",
                Description = "3-wall U-shape layout with an open southern entrance for walk-through access.",
                IsBuiltIn = true,
                Options = new RoomTemplateOptions
                {
                    Sides = 3,
                    OpenSide = WallSide.South,
                    PcCount = 8,
                    IncludeDesks = true,
                    IncludeChairs = true,
                    GroupElements = true
                }
            },
            new()
            {
                Id = "preset_6_pc_corner",
                Name = "6-PC L-Corner Lab",
                Description = "2-wall corner perimeter ideal for corner rooms, perimeter rows, or office cubicles.",
                IsBuiltIn = true,
                Options = new RoomTemplateOptions
                {
                    Sides = 2,
                    CornerOption = "NorthWest",
                    PcCount = 6,
                    IncludeDesks = true,
                    IncludeChairs = true,
                    GroupElements = true
                }
            },
            new()
            {
                Id = "preset_4_pc_bench",
                Name = "4-PC Single Bench",
                Description = "Single-wall linear bench layout with 4 in-line modular desks.",
                IsBuiltIn = true,
                Options = new RoomTemplateOptions
                {
                    Sides = 1,
                    SingleSide = WallSide.North,
                    PcCount = 4,
                    IncludeDesks = true,
                    IncludeChairs = true,
                    GroupElements = true
                }
            }
        };
    }
}
