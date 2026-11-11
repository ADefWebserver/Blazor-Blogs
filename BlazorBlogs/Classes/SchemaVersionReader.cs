using Microsoft.Data.SqlClient;

namespace BlazorBlogs;

public enum SchemaVersionOutcome
{
    Fresh,
    Found,
    UnexpectedState,
    Failed
}

public sealed record SchemaVersionResult(
    SchemaVersionOutcome Outcome,
    string Version = "00.00.00",
    string Error = "");

public static class SchemaVersionReader
{
    public static async Task<SchemaVersionResult> ReadAsync(string connectionString)
    {
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT [SettingValue] FROM [dbo].[Settings] WHERE [SettingName] = N'VersionNumber'";

            var value = await command.ExecuteScalarAsync();
            return value is null
                ? new SchemaVersionResult(
                    SchemaVersionOutcome.UnexpectedState,
                    Error: "The database contains a Settings table but no VersionNumber row.")
                : new SchemaVersionResult(SchemaVersionOutcome.Found, Convert.ToString(value) ?? "");
        }
        catch (SqlException ex) when (ex.Number == 208)
        {
            return new SchemaVersionResult(SchemaVersionOutcome.Fresh);
        }
        catch (Exception ex)
        {
            return new SchemaVersionResult(
                SchemaVersionOutcome.Failed,
                Error: ex.GetBaseException().Message);
        }
    }
}
