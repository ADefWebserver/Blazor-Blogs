using BlazorBlogs.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Runtime.Loader;
using Toolbelt.Blazor.Extensions.DependencyInjection;
using WilderMinds.MetaWeblog;

using BlazorBlogs.Components;
using BlazorBlogs.Components.Account;
using Blazored.Toast;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Radzen;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.Options;


namespace BlazorBlogs
{
    public class Program
    {
        public IConfiguration Configuration { get; }
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            ConnectionStringStore.MigrateFromAppSettings(builder.Environment.ContentRootPath);
            builder.AddServiceDefaults();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // Read the connection string from the appsettings.json file
            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile(ConnectionStringStore.FileName, optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            // Allows appsettings.json to be updated programatically
            builder.Services.ConfigureWritable<ConnectionStrings>(builder.Configuration.GetSection("ConnectionStrings"));
            builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddScoped<IdentityUserAccessor>();
            builder.Services.AddScoped<IdentityRedirectManager>();
            builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

            var authenticationBuilder = builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            });

            authenticationBuilder.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = ExternalAuthOptionsStore.PlaceholderClientId;
                options.ClientSecret = ExternalAuthOptionsStore.PlaceholderClientSecret;
            });
            authenticationBuilder.AddIdentityCookies();

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            BlazorBlogsDbOptions.Configure(options, connectionString, builder.Configuration));

            builder.Services.AddDbContext<BlazorBlogsContext>(options =>
            BlazorBlogsDbOptions.Configure(options, connectionString, builder.Configuration));

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
            })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddSignInManager()
                .AddRoleManager<RoleManager<IdentityRole>>()
                .AddRoleStore<RoleStore<IdentityRole, ApplicationDbContext>>()
                .AddDefaultTokenProviders();

            builder.Services.AddScoped<BlogsService>();
            builder.Services.AddScoped<GeneralSettingsService>();
            builder.Services.AddScoped<EmailService>();
            builder.Services.AddScoped<IEmailSender<ApplicationUser>, EmailSender>();
            builder.Services.AddScoped<SearchState>();
            builder.Services.AddScoped<SiteUrlService>();
            builder.Services.AddScoped<NewsletterSubscriptionService>();
            builder.Services.AddScoped<CampaignSender>();
            builder.Services.AddSingleton<CampaignSendRegistry>();
            builder.Services.AddScoped<GeneralSettingsState>();
            builder.Services.AddScoped<StripeSettingsService>();
            builder.Services.AddScoped<GoogleAuthSettingsService>();
            builder.Services.AddSingleton<ExternalAuthOptionsStore>();
            builder.Services.AddSingleton<IPostConfigureOptions<GoogleOptions>>(
                services => services.GetRequiredService<ExternalAuthOptionsStore>());
            builder.Services.AddScoped<IStripeGateway, StripeGateway>();
            builder.Services.AddScoped<PaidDownloadService>();
            builder.Services.AddSingleton<DownloadStorageService>();
            builder.Services.AddSingleton<UserContentPaths>();
            builder.Services.AddScoped<DisqusState>();
            builder.Services.AddScoped<InstallUpdateState>();
            builder.Services.AddMetaWeblog<BlazorBlogs.MetaWeblogService>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<HttpContextAccessor>();
            builder.Services.AddScoped<HttpClient>();
            builder.Services.AddBlazoredToast();
            builder.Services.AddHeadElementHelper();

            builder.Services.AddRadzenComponents();

            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            try
            {
                using var scope = app.Services.CreateScope();
                var googleSettings = await scope.ServiceProvider
                    .GetRequiredService<GoogleAuthSettingsService>()
                    .GetAsync();
                scope.ServiceProvider
                    .GetRequiredService<ExternalAuthOptionsStore>()
                    .UpdateGoogle(googleSettings);
            }
            catch (Exception ex)
            {
                app.Logger.LogWarning(
                    ex,
                    "Google sign-in settings could not be loaded; Google sign-in remains disabled.");
            }

            app.Logger.LogInformation(
                "Database connection source: {Source}. {Reason}",
                ConnectionStringStore.Status,
                ConnectionStringStore.StatusReason);

            app.UseHeadElementServerPrerendering();
            app.UseMetaWeblog("/MetaWeblog");

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.MapDefaultEndpoints();

            app.UseAntiforgery();

            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/files", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Redirect("/filedownloads");
                    return;
                }

                await next();
            });

            var userContentPaths = app.Services.GetRequiredService<UserContentPaths>();
            userContentPaths.EnsureFoldersAndSeed(app.Logger);
            app.Services.GetRequiredService<DownloadStorageService>().MigrateLegacyFiles();

            if (!string.Equals(
                    userContentPaths.PublicRoot,
                    app.Environment.WebRootPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(userContentPaths.PublicRoot)
                });
            }

            app.UseStaticFiles();
            app.MapStaticAssets();

            app.MapControllers();

            app.MapRazorComponents<Components.App>()
                .AddInteractiveServerRenderMode();

            // Add additional endpoints required by the Identity /Account Razor components.
            app.MapAdditionalIdentityEndpoints();

            await app.RunAsync();
        }
    }
}