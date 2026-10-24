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
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        // 1. Detect table name (try 'devices', 'public.devices', 'device')
        const string checkTableSql = @"
            SELECT table_name 
            FROM information_schema.tables 
            WHERE table_schema NOT IN ('information_schema', 'pg_catalog')
              AND lower(table_name) IN ('devices', 'device', 'inventory_devices', 'lab_devices')
            LIMIT 1;";

        var tableName = await conn.QueryFirstOrDefaultAsync<string>(checkTableSql);
        if (string.IsNullOrEmpty(tableName))
        {
            // Fallback: check if standard devices query works directly
            try
            {
                var standardRows = await conn.QueryAsync<DatabaseDevice>(@"
                    SELECT 
                        device_id AS DeviceId,
                        display_name AS DisplayName,
                        primary_ip AS PrimaryIp,
                        mac_address AS MacAddress,
                        COALESCE(is_assigned, false) AS IsAssigned
                    FROM devices
                    ORDER BY display_name ASC;");
                return standardRows.ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Table 'devices' was not found in the PostgreSQL database.\n\n" +
                    "Please ensure your database contains a table named 'devices' with columns:\n" +
                    "• device_id (text/varchar)\n" +
                    "• display_name (text/varchar)\n" +
                    "• primary_ip (text/varchar)\n" +
                    "• mac_address (text/varchar)\n\n" +
                    $"Database error: {ex.Message}", ex);
            }
        }

        // 2. Discover available columns to map flexibly
        const string colsSql = @"
            SELECT lower(column_name) 
            FROM information_schema.columns 
            WHERE lower(table_name) = lower(@tableName);";

        var colList = (await conn.QueryAsync<string>(colsSql, new { tableName })).ToList();
        var colSet = new HashSet<string>(colList, StringComparer.OrdinalIgnoreCase);

        // Identify device_id column
        string idCol = colSet.Contains("device_id") ? "device_id" :
                       colSet.Contains("id") ? "id" :
                       colSet.Contains("asset_id") ? "asset_id" :
                       colSet.Contains("dev_id") ? "dev_id" : "1::text";

        // Identify display_name column
        string nameCol = colSet.Contains("display_name") ? "display_name" :
                         colSet.Contains("name") ? "name" :
                         colSet.Contains("hostname") ? "hostname" :
                         colSet.Contains("device_name") ? "device_name" :
                         colSet.Contains("title") ? "title" : idCol;

        // Identify primary_ip column
        string ipCol = colSet.Contains("primary_ip") ? "primary_ip" :
                       colSet.Contains("ip_address") ? "ip_address" :
                       colSet.Contains("ip") ? "ip" :
                       colSet.Contains("ipv4") ? "ipv4" : "''";

        // Identify mac_address column
        string macCol = colSet.Contains("mac_address") ? "mac_address" :
                        colSet.Contains("mac") ? "mac" :
                        colSet.Contains("hardware_address") ? "hardware_address" : "''";

        // Identify is_assigned column
        string assignedCol = colSet.Contains("is_assigned") ? "COALESCE(is_assigned, false)" :
                             colSet.Contains("assigned") ? "COALESCE(assigned, false)" : "false";

        string query = $@"
            SELECT 
                {idCol}::text AS DeviceId,
                {nameCol}::text AS DisplayName,
                {ipCol}::text AS PrimaryIp,
                {macCol}::text AS MacAddress,
                {assignedCol}::boolean AS IsAssigned
            FROM {tableName}
            ORDER BY {nameCol} ASC;";

        var rows = await conn.QueryAsync<DatabaseDevice>(query);
        return rows.ToList();
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
