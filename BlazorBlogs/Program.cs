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


namespace BlazorBlogs
{
    public class Program
    {
        public IConfiguration Configuration { get; }
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // Read the connection string from the appsettings.json file
            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            // Allows appsettings.json to be updated programatically
            builder.Services.ConfigureWritable<ConnectionStrings>(builder.Configuration.GetSection("ConnectionStrings"));
            builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddScoped<IdentityUserAccessor>();
            builder.Services.AddScoped<IdentityRedirectManager>();
            builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
                .AddIdentityCookies();

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

            builder.Services.AddDbContext<BlazorBlogsContext>(options =>
            options.UseSqlServer(connectionString));

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

            app.UseAntiforgery();

            app.MapStaticAssets();

            app.MapControllers();

            app.MapRazorComponents<Components.App>()
                .AddInteractiveServerRenderMode();

            // Add additional endpoints required by the Identity /Account Razor components.
            app.MapAdditionalIdentityEndpoints();

            app.Run();
        }
    }
}