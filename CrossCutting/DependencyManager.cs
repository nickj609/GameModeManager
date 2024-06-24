// Included libraries
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

// Declare namespace
namespace GameModeManager
{
    // Define class
    public class DependencyManager<TPlugin, TConfig>
    {
        // Define dependencies
        private List<Type> TypesToAdd { get; set; } = new();
        Type dependencyType = typeof(IPluginDependency<TPlugin, TConfig>);
        private List<IPluginDependency<TPlugin, TConfig>> Dependencies { get; set; } = new();

        // Define method to load dependencies
        public void LoadDependencies(Assembly assembly)
        {

            var typesToAdd = assembly.GetTypes()
                .Where(x => x.IsClass)
                .Where(dependencyType.IsAssignableFrom);

            TypesToAdd.AddRange(typesToAdd);
        }

        // Define method to create singletons
        public void AddIt(IServiceCollection collection)
        {
            foreach (var type in TypesToAdd)
            {
                collection.AddSingleton(type);
            }

            collection.AddSingleton(p =>
            {
                Dependencies = TypesToAdd
                    .Where(x => dependencyType.IsAssignableFrom(x))
                    .Select(type => (IPluginDependency<TPlugin, TConfig>)p.GetService(type)!)
                    .ToList();

                return this;
            });
        }

        // Define on map start behavior
        public void OnMapStart(string mapName)
        {
            foreach (var service in Dependencies)
            {
                service.OnMapStart(mapName);
            }
        }

        // Define on plugin load behavior
        public void OnPluginLoad(TPlugin plugin)
        {
            foreach (var service in Dependencies)
            {
                service.OnLoad(plugin);
            }
        }

        // Define on config parsed behavior
        public void OnConfigParsed(TConfig config)
        {
            foreach (var service in Dependencies)
            {
                service.OnConfigParsed(config);
            }
        }
    }
}