using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LabScape.Core.Models;

namespace LabScape.Infrastructure.Services;

public class DatabaseConnectionProfile
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 5432;
    public string Database { get; set; } = "network_observer";
    public string Username { get; set; } = "postgres";
    public string Password { get; set; } = string.Empty;
    public bool UseMock { get; set; } = false;
    public bool IsConfigured { get; set; } = false;
    public DateTime? LastConnected { get; set; }

    public string BuildConnectionString()
    {
        return $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password};Timeout=5;CommandTimeout=5;";
    }
}

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class DpapiCredentialVault
{
    public static string? CustomVaultPath { get; set; }
    public static string? CustomCachePath { get; set; }

    public static string VaultPath => !string.IsNullOrEmpty(CustomVaultPath)
        ? CustomVaultPath
        : Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LabScape",
            "db_settings.dat");

    public static string CachePath => !string.IsNullOrEmpty(CustomCachePath)
        ? CustomCachePath
        : Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LabScape",
            "inventory_cache.json");

    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("LabScape-Enterprise-DPAPI-Entropy");

    public static void SaveProfile(DatabaseConnectionProfile profile)
    {
        var dir = Path.GetDirectoryName(VaultPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string json = JsonSerializer.Serialize(profile);
        byte[] plaintextBytes = Encoding.UTF8.GetBytes(json);
        byte[] encryptedBytes = ProtectedData.Protect(plaintextBytes, Entropy, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(VaultPath, encryptedBytes);
    }

    public static DatabaseConnectionProfile LoadProfile()
    {
        if (!File.Exists(VaultPath))
        {
            return new DatabaseConnectionProfile();
        }

        try
        {
            byte[] encryptedBytes = File.ReadAllBytes(VaultPath);
            byte[] plaintextBytes = ProtectedData.Unprotect(encryptedBytes, Entropy, DataProtectionScope.CurrentUser);
            string json = Encoding.UTF8.GetString(plaintextBytes);
            return JsonSerializer.Deserialize<DatabaseConnectionProfile>(json) ?? new DatabaseConnectionProfile();
        }
        catch
        {
            return new DatabaseConnectionProfile();
        }
    }

    public static void SaveInventoryCache(IReadOnlyList<DatabaseDevice> devices)
    {
        try
        {
            var dir = Path.GetDirectoryName(CachePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonSerializer.Serialize(devices);
            File.WriteAllText(CachePath, json);
        }
        catch { }
    }

    public static IReadOnlyList<DatabaseDevice> LoadInventoryCache()
    {
        try
        {
            if (!File.Exists(CachePath)) return Array.Empty<DatabaseDevice>();
            string json = File.ReadAllText(CachePath);
            return JsonSerializer.Deserialize<List<DatabaseDevice>>(json) ?? (IReadOnlyList<DatabaseDevice>)Array.Empty<DatabaseDevice>();
        }
        catch
        {
            return Array.Empty<DatabaseDevice>();
        }
    }
}
