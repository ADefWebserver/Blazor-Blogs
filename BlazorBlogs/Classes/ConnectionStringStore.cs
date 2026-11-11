using System.Text.Json;
using System.Text.Json.Nodes;

namespace BlazorBlogs;

public enum ConnectionSource
{
    None,
    HostManaged,
    LocalFile,
    MigratedNow,
    AppSettingsOnly
}

public static class ConnectionStringStore
{
    public const string FileName = "appsettings.Local.json";

    private static readonly string[] HostManagedVariableNames =
    [
        "ConnectionStrings__DefaultConnection",
        "SQLAZURECONNSTR_DefaultConnection",
        "SQLCONNSTR_DefaultConnection",
        "CUSTOMCONNSTR_DefaultConnection"
    ];

    public static ConnectionSource Status { get; private set; }
    public static string StatusReason { get; private set; } = string.Empty;

    public static bool IsHostManaged(IConfiguration configuration)
    {
        return !configuration.GetValue("Install:AllowDatabaseConfiguration", true)
            || HostManagedVariableNames.Any(name =>
                !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)));
    }

    public static void MigrateFromAppSettings(string contentRootPath)
    {
        StatusReason = string.Empty;

        if (HostManagedVariableNames.Any(name =>
                !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name))))
        {
            Status = ConnectionSource.HostManaged;
            return;
        }

        var localFilePath = Path.Combine(contentRootPath, FileName);
        if (File.Exists(localFilePath))
        {
            Status = ConnectionSource.LocalFile;
            return;
        }

        var appSettingsPath = Path.Combine(contentRootPath, "appsettings.json");
        if (!File.Exists(appSettingsPath))
        {
            Status = ConnectionSource.None;
            return;
        }

        try
        {
            var root = JsonNode.Parse(File.ReadAllText(appSettingsPath)) as JsonObject;
            var connectionString = root?["ConnectionStrings"]?["DefaultConnection"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                Status = ConnectionSource.None;
                return;
            }

            Save(contentRootPath, connectionString);
            Status = ConnectionSource.MigratedNow;
        }
        catch (Exception ex)
        {
            Status = ConnectionSource.AppSettingsOnly;
            StatusReason = ex.GetBaseException().Message;
        }
    }

    public static void Save(string contentRootPath, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRootPath);

        var payload = new JsonObject
        {
            ["ConnectionStrings"] = new JsonObject
            {
                ["DefaultConnection"] = connectionString
            }
        };

        var path = Path.Combine(contentRootPath, FileName);
        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";

        File.WriteAllText(
            temporaryPath,
            payload.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        File.Move(temporaryPath, path, true);
    }
}
