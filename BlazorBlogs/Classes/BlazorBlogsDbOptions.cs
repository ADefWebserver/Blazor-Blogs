using Microsoft.EntityFrameworkCore;

namespace BlazorBlogs;

public static class BlazorBlogsDbOptions
{
    public static void Configure(
        DbContextOptionsBuilder builder,
        string connectionString,
        bool retry = true)
    {
        var environmentConfiguration = new ConfigurationManager();
        environmentConfiguration.AddEnvironmentVariables();
        Configure(builder, connectionString, environmentConfiguration, retry);
    }

    public static void Configure(
        DbContextOptionsBuilder builder,
        string connectionString,
        IConfiguration configuration,
        bool retry = true)
    {
        builder.UseSqlServer(connectionString ?? string.Empty, sqlOptions =>
        {
            if (retry)
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 2,
                    maxRetryDelay: TimeSpan.FromSeconds(2),
                    errorNumbersToAdd: null);
            }

            if (int.TryParse(configuration["Database:CompatibilityLevel"], out var compatibilityLevel))
            {
                sqlOptions.UseCompatibilityLevel(compatibilityLevel);
            }
        });
    }
}
