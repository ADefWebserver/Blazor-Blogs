using System.Text.Json;

namespace BlazorBlogs.Tests;

public sealed class ConnectionStringStoreTests
{
    [Fact]
    public void MigrateFromAppSettings_CopiesConnectionWithoutChangingAppSettings()
    {
        var root = Path.Combine(
            AppContext.BaseDirectory,
            "connection-store",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var appSettingsPath = Path.Combine(root, "appsettings.json");
            var appSettings = """
                {
                  "ConnectionStrings": {
                    "DefaultConnection": "Server=preserved;Database=BlazorBlogs"
                  }
                }
                """;
            File.WriteAllText(appSettingsPath, appSettings);

            ConnectionStringStore.MigrateFromAppSettings(root);

            Assert.Equal(ConnectionSource.MigratedNow, ConnectionStringStore.Status);
            Assert.True(File.Exists(Path.Combine(root, ConnectionStringStore.FileName)));
            Assert.Equal(appSettings, File.ReadAllText(appSettingsPath));

            using var local = JsonDocument.Parse(File.ReadAllText(
                Path.Combine(root, ConnectionStringStore.FileName)));
            Assert.Equal(
                "Server=preserved;Database=BlazorBlogs",
                local.RootElement
                    .GetProperty("ConnectionStrings")
                    .GetProperty("DefaultConnection")
                    .GetString());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void MigrateFromAppSettings_DoesNotOverwriteExistingLocalFile()
    {
        var root = Path.Combine(
            AppContext.BaseDirectory,
            "connection-store",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            File.WriteAllText(
                Path.Combine(root, ConnectionStringStore.FileName),
                """{"ConnectionStrings":{"DefaultConnection":"original"}}""");
            File.WriteAllText(
                Path.Combine(root, "appsettings.json"),
                """{"ConnectionStrings":{"DefaultConnection":"replacement"}}""");

            ConnectionStringStore.MigrateFromAppSettings(root);

            Assert.Equal(ConnectionSource.LocalFile, ConnectionStringStore.Status);
            Assert.Contains(
                "original",
                File.ReadAllText(Path.Combine(root, ConnectionStringStore.FileName)));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
