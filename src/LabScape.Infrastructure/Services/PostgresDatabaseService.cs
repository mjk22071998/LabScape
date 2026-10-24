using LabScape.Core.Interfaces;
using LabScape.Core.Models;
using LabScape.Infrastructure.Repositories;

namespace LabScape.Infrastructure.Services;

public class PostgresDatabaseService : IDatabaseService
{
    private readonly IDeviceRepository _deviceRepository;

    public PostgresDatabaseService(IDeviceRepository? deviceRepository = null)
    {
        _deviceRepository = deviceRepository ?? new PostgreSqlDeviceRepository();
    }

    public Task<bool> TestConnectionAsync(string connectionString)
    {
        return _deviceRepository.TestConnectionAsync(connectionString);
    }

    public Task<IReadOnlyList<DatabaseDevice>> GetInventoryDevicesAsync(string connectionString)
    {
        return _deviceRepository.GetDevicesAsync(connectionString);
    }
}
