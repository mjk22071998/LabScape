using LayoutDesigner.Core.Models;

namespace LayoutDesigner.Core.Interfaces;

public interface ILayoutRepository
{
    Task<LayoutDocument> LoadFromFileAsync(string filePath);
    Task SaveToFileAsync(LayoutDocument document, string filePath);
    string SerializeToJson(LayoutDocument document);
    LayoutDocument DeserializeFromJson(string json);
}
