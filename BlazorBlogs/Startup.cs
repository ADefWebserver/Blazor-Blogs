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
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var path = Path.GetFullPath(@"CustomModules\BlazorBlogsLibrary.dll");

            var BlazorBlogsAssembly =
                AssemblyLoadContext
                .Default.LoadFromAssemblyPath(path);

            Type BlazorBlogsType =
                BlazorBlogsAssembly
                .GetType("Microsoft.Extensions.DependencyInjection.RegisterServices");

            // Add framework services.
            services.AddMvc(options => options.EnableEndpointRouting = true)
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
