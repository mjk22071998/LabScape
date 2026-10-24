using LayoutDesigner.Core.Models;

namespace LayoutDesigner.Core.Interfaces;

public interface IDatabaseService
{
    Task<bool> TestConnectionAsync(string connectionString);
    Task<IReadOnlyList<DatabaseDevice>> GetInventoryDevicesAsync(string connectionString);
}
