using LabScape.Core.Models;

namespace LabScape.Core.Interfaces;

public interface IDeviceRepository
{
    Task<bool> TestConnectionAsync(string connectionString);
    Task<TimeSpan> MeasureLatencyAsync(string connectionString);
    Task<IReadOnlyList<DatabaseDevice>> GetDevicesAsync(string connectionString);
}
