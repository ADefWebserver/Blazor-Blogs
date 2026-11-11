using System.Diagnostics;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Pipelines;
using Azure.Provisioning.Sql;

var builder = DistributedApplication.CreateBuilder(args);

var identity = builder.ExecutionContext.IsRunMode
    ? null
    : builder.AddAzureUserAssignedIdentity("blazorblogs-identity");

var sql = builder.AddAzureSqlServer("sql")
    .ConfigureInfrastructure(infrastructure =>
    {
        var database = infrastructure.GetProvisionableResources()
            .OfType<SqlDatabase>()
            .Single();

        database.Sku = new SqlSku
        {
            Name = "Basic",
            Tier = "Basic"
        };
        database.UseFreeLimit = false;
        database.FreeLimitExhaustionBehavior = FreeLimitExhaustionBehavior.BillOverUsage;
    })
    .RunAsContainer(container => container
        .WithDataVolume()
        .WithLifetime(ContainerLifetime.Persistent));

var database = sql.AddDatabase("blazorblogsdb");

if (builder.ExecutionContext.IsRunMode)
{
    builder.AddProject<Projects.BlazorBlogs>("blazorblogs")
        .WithReference(database, connectionName: "DefaultConnection")
        .WaitFor(database)
        .WithEnvironment("Install__AllowDatabaseConfiguration", "false")
        .WithExternalHttpEndpoints();
}
else
{
    var sqlAdmin = builder.AddBicepTemplate("sql-admin", "../infra/sql-admin.bicep")
        .WithParameter("sqlServerName", sql.Resource.NameOutputReference)
        .WithParameter("identityName", "blazorblogs-identity")
        .WithParameter("identityPrincipalId", identity!.Resource.PrincipalId);

    var website = builder.AddBicepTemplate("website", "../infra/website.bicep")
        .WithParameter("websiteName", builder.AddParameter("websiteName"))
        .WithParameter(
            "appServiceSku",
            builder.AddParameter("appServiceSku", "B1", publishValueAsDefault: true)
                .WithDescription("App Service plan SKU. Use B1 or higher for Always On."))
        .WithParameter("identityId", identity.Resource.Id)
        .WithParameter("identityClientId", identity.Resource.ClientId)
        .WithParameter("sqlConnectionString", database)
        .WithParameter("sqlAdminReady", sqlAdmin.GetOutput("configured"));

    builder.Pipeline.AddStep(
        "deploy-website-code",
        async context =>
        {
            var websiteName = await website.GetOutput("websiteName")
                .GetValueAsync(context.CancellationToken);
            if (string.IsNullOrWhiteSpace(websiteName))
            {
                throw new InvalidOperationException(
                    "The website provisioning step did not return the websiteName output.");
            }

            var script = Path.GetFullPath(
                Path.Combine(builder.AppHostDirectory, "..", "deploy", "Deploy-Website.ps1"));

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo("pwsh")
                {
                    ArgumentList =
                    {
                        "-NoProfile",
                        "-NonInteractive",
                        "-File", script,
                        "-WebsiteName", websiteName
                    },
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.OutputDataReceived += (_, eventArgs) =>
            {
                if (eventArgs.Data is not null)
                {
                    Console.WriteLine(eventArgs.Data);
                }
            };
            process.ErrorDataReceived += (_, eventArgs) =>
            {
                if (eventArgs.Data is not null)
                {
                    Console.Error.WriteLine(eventArgs.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync(context.CancellationToken);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Website code deployment failed with exit code {process.ExitCode}.");
            }
        },
        dependsOn: "provision-website",
        requiredBy: WellKnownPipelineSteps.Deploy);
}

builder.Build().Run();
