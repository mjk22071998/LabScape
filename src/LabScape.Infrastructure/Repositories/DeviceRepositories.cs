using System.Diagnostics;
using Dapper;
using LabScape.Core.Interfaces;
using LabScape.Core.Models;
using Npgsql;

namespace LabScape.Infrastructure.Repositories;

public class PostgreSqlDeviceRepository : IDeviceRepository
{
    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT 1", conn);
            var res = await cmd.ExecuteScalarAsync();
            return res != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<TimeSpan> MeasureLatencyAsync(string connectionString)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT 1", conn);
            await cmd.ExecuteScalarAsync();
            sw.Stop();
            return sw.Elapsed;
        }
        catch
        {
            sw.Stop();
            return TimeSpan.FromMilliseconds(-1);
        }
    }

    public async Task<IReadOnlyList<DatabaseDevice>> GetDevicesAsync(string connectionString)
    {
        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            const string sql = @"
                SELECT 
                    device_id AS DeviceId,
                    display_name AS DisplayName,
                    primary_ip AS PrimaryIp,
                    mac_address AS MacAddress,
                    COALESCE(is_assigned, false) AS IsAssigned
                FROM public.devices
                ORDER BY display_name ASC";

            var rows = await conn.QueryAsync<DatabaseDevice>(sql);
            return rows.ToList();
        }
        catch
        {
            return Array.Empty<DatabaseDevice>();
        }
    }
}

public class MockDeviceRepository : IDeviceRepository
{
    private readonly List<DatabaseDevice> _mockDevices = new();

    public MockDeviceRepository()
    {
        // 25 simulated lab workstations & infrastructure devices
        for (int i = 1; i <= 15; i++)
        {
            _mockDevices.Add(new DatabaseDevice
            {
                DeviceId = $"LAB-CAD-{i:D2}",
                DisplayName = $"CAD-Workstation-{i:D2}",
                PrimaryIp = $"192.168.10.{100 + i}",
                MacAddress = $"00:1B:44:11:3A:{i:X2}",
                IsAssigned = false
            });
        }

        for (int i = 1; i <= 6; i++)
        {
            _mockDevices.Add(new DatabaseDevice
            {
                DeviceId = $"LAB-SRV-{i:D2}",
                DisplayName = $"Server-Node-{i:D2}",
                PrimaryIp = $"10.0.5.{10 + i}",
                MacAddress = $"52:54:00:12:34:{i:X2}",
                IsAssigned = false
            });
        }

        _mockDevices.Add(new DatabaseDevice
        {
            DeviceId = "SW-CORE-01",
            DisplayName = "Core-Switch-RackA",
            PrimaryIp = "10.0.5.1",
            MacAddress = "A4:93:4C:01:02:03",
            IsAssigned = false
        });
        _mockDevices.Add(new DatabaseDevice
        {
            DeviceId = "PRN-OFFICE-01",
            DisplayName = "LaserJet-Enterprise-Color",
            PrimaryIp = "192.168.10.250",
            MacAddress = "70:5A:0F:88:99:AA",
            IsAssigned = false
        });
        _mockDevices.Add(new DatabaseDevice
        {
            DeviceId = "CONF-AV-HUB",
            DisplayName = "Conference-Room-Display-Hub",
            PrimaryIp = "192.168.10.251",
            MacAddress = "B8:27:EB:44:55:66",
            IsAssigned = false
        });
        _mockDevices.Add(new DatabaseDevice
        {
            DeviceId = "DEV-GATEWAY",
            DisplayName = "Campus-Firewall-Gateway",
            PrimaryIp = "192.168.10.1",
            MacAddress = "00:0C:29:77:88:99",
            IsAssigned = false
        });
    }

    public Task<bool> TestConnectionAsync(string connectionString)
    {
        return Task.FromResult(true);
    }

    public Task<TimeSpan> MeasureLatencyAsync(string connectionString)
    {
        return Task.FromResult(TimeSpan.FromMilliseconds(2.4));
    }

    public Task<IReadOnlyList<DatabaseDevice>> GetDevicesAsync(string connectionString)
    {
        return Task.FromResult<IReadOnlyList<DatabaseDevice>>(_mockDevices);
    }
}
