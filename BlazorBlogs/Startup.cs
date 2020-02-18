using System;
using System.IO;
using System.Runtime.Loader;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Toolbelt.Blazor.Extensions.DependencyInjection;
using WilderMinds.MetaWeblog;

namespace BlazorBlogs
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;

            // Before we load the CustomClassLibrary.dll (and potentially lock it)
            // Determine if we have files in the Upgrade directory and process it first
            if (System.IO.File.Exists(env.ContentRootPath + @"\Upgrade\BlazorBlogsLibrary.dll"))
            {
                // Delete current 
                System.IO.File.Delete(env.ContentRootPath + @"\CustomModules\BlazorBlogsLibrary.dll");
                System.IO.File.Delete(env.ContentRootPath + @"\CustomModules\BlazorBlogsLibrary.Views.dll");

                // Copy new 
                System.IO.File.Copy(
                    env.ContentRootPath + @"\Upgrade\BlazorBlogsLibrary.dll",
                    env.ContentRootPath + @"\CustomModules\BlazorBlogsLibrary.dll");
                System.IO.File.Copy(
                    env.ContentRootPath + @"\Upgrade\BlazorBlogsLibrary.Views.dll",
                    env.ContentRootPath + @"\CustomModules\BlazorBlogsLibrary.Views.dll");

                // Delete Upgrade - so it wont be processed again
                System.IO.File.Delete(env.ContentRootPath + @"\Upgrade\BlazorBlogsLibrary.dll");
                System.IO.File.Delete(env.ContentRootPath + @"\Upgrade\BlazorBlogsLibrary.Views.dll");
            }
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var BlazorBlogsLibraryViewsPath = Path.GetFullPath(@"CustomModules\BlazorBlogsLibrary.Views.dll");

            var BlazorBlogsViewsAssembly =
                AssemblyLoadContext
                .Default.LoadFromAssemblyPath(BlazorBlogsLibraryViewsPath);

            var BlazorBlogsLibraryPath = Path.GetFullPath(@"CustomModules\BlazorBlogsLibrary.dll");

            var BlazorBlogsAssembly =
                AssemblyLoadContext
                .Default.LoadFromAssemblyPath(BlazorBlogsLibraryPath);

            Type BlazorBlogsType =
                BlazorBlogsAssembly
                .GetType("Microsoft.Extensions.DependencyInjection.RegisterServices");

            services.AddMvc(options => options.EnableEndpointRouting = false)
                .AddApplicationPart(BlazorBlogsViewsAssembly)
                .AddApplicationPart(BlazorBlogsAssembly);

            BlazorBlogsType.GetMethod("AddBlazorBlogsServices")
                .Invoke(null, new object[] { services, Configuration });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. 
                // You may want to change this for production scenarios, 
                // see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHeadElementServerPrerendering();
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseMetaWeblog("/MetaWeblog");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
