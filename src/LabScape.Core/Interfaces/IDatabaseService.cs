using LabScape.Core.Models;

namespace LabScape.Core.Interfaces;

public interface IDatabaseService
{
    Task<bool> TestConnectionAsync(string connectionString);
    Task<IReadOnlyList<DatabaseDevice>> GetInventoryDevicesAsync(string connectionString);
}
