using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

// From: https://stackoverflow.com/questions/40970944/how-to-update-values-into-appsetting-json
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void ConfigureWritable<T>(
            this IServiceCollection services,
            IConfigurationSection section,
            string file = "appsettings.json") where T : class, new()
        {
            services.Configure<T>(section);
            services.AddTransient<IWritableOptions<T>>(provider =>
            {
                var environment = provider.GetService<IWebHostEnvironment>();
                var options = provider.GetService<IOptionsMonitor<T>>();
                return new WritableOptions<T>(environment, options, section.Key, file);
            });
        }
    }

    public interface IWritableOptions<out T> : IOptionsSnapshot<T> where T : class, new()
    {
        void Update(Action<T> applyChanges);
    }

    public class WritableOptions<T> : IWritableOptions<T> where T : class, new()
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IOptionsMonitor<T> _options;
        private readonly string _section;
        private readonly string _file;

        public WritableOptions(
            IWebHostEnvironment environment,
            IOptionsMonitor<T> options,
            string section,
            string file)
        {
            _environment = environment;
            _options = options;
            _section = section;
            _file = file;
        }

        public T Value => _options.CurrentValue;
        public T Get(string name) => _options.Get(name);

        public void Update(Action<T> applyChanges)
        {
            var fileProvider = _environment.ContentRootFileProvider;
            var fileInfo = fileProvider.GetFileInfo(_file);
            var physicalPath = fileInfo.PhysicalPath;

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            var jObject = JsonNode.Parse(File.ReadAllText(physicalPath))?.AsObject() ?? new JsonObject();

            var sectionObject = jObject.TryGetPropertyValue(_section, out JsonNode section) && section != null
                ? JsonSerializer.Deserialize<T>(section.ToJsonString())
                : (Value ?? new T());

            applyChanges(sectionObject);

            jObject[_section] = JsonSerializer.SerializeToNode(sectionObject, jsonOptions);
            File.WriteAllText(physicalPath, JsonSerializer.Serialize(jObject, jsonOptions));
        }
    }
}
