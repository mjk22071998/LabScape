using Dapper;
using LayoutDesigner.Core.Interfaces;
using LayoutDesigner.Core.Models;
using Npgsql;

namespace LayoutDesigner.Infrastructure.Services;

public class PostgresDatabaseService : IDatabaseService
{
    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT 1", connection);
            var result = await cmd.ExecuteScalarAsync();
            return result != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<DatabaseDevice>> GetInventoryDevicesAsync(string connectionString)
    {
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            const string query = @"
                SELECT 
                    device_id AS DeviceId,
                    display_name AS DisplayName,
                    primary_ip AS PrimaryIp,
                    mac_address AS MacAddress,
                    COALESCE(is_assigned, false) AS IsAssigned
                FROM public.devices
                ORDER BY display_name ASC";

            var rows = await connection.QueryAsync<DatabaseDevice>(query);
            return rows.ToList();
        }
        catch
        {
            return Array.Empty<DatabaseDevice>();
        }
    }
}
