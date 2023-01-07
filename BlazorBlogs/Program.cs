using BlazorBlogs.Areas.Identity;
using BlazorBlogs.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Runtime.Loader;
using Toolbelt.Blazor.Extensions.DependencyInjection;
using WilderMinds.MetaWeblog;

namespace BlazorBlogs
{
    public class Program
    {
        public IConfiguration Configuration { get; }
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Read the connection string from the appsettings.json file
            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            // Get HostingEnvironment
            var env = builder.Environment;

            var BlazorBlogsLibraryPath = Path.GetFullPath(@"CustomModules\BlazorBlogsLibrary.dll");

            var BlazorBlogsAssembly =
                AssemblyLoadContext
                .Default.LoadFromAssemblyPath(BlazorBlogsLibraryPath);

            Type BlazorBlogsType =
                BlazorBlogsAssembly
                .GetType("Microsoft.Extensions.DependencyInjection.RegisterServices");

            builder.Services.AddMvc(options => options.EnableEndpointRouting = false)
                .AddApplicationPart(BlazorBlogsAssembly);
            
            BlazorBlogsType.GetMethod("AddBlazorBlogsServices")
                .Invoke(null, new object[] { builder });

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

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllers();
            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            app.Run();
        }
    }
}