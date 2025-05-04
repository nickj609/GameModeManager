// Included libraries
using System.Reflection;
using GameModeManager.Contracts;
using Microsoft.Extensions.DependencyInjection;

// Declare namespace
namespace GameModeManager.CrossCutting
{
    // Define class
    public class DependencyManager<TPlugin, TConfig>
    {
        // Define class properties
        private List<Type> TypesToAdd { get; set; } = new();
        Type dependencyType = typeof(IPluginDependency<TPlugin, TConfig>);
        private List<IPluginDependency<TPlugin, TConfig>> Dependencies { get; set; } = new();

        // Define class methods
        public void OnConfigParsed(TConfig config)
        {
            foreach (var service in Dependencies)
            {
                service.OnConfigParsed(config);
            }
        }

        public void OnPluginLoad(TPlugin plugin)
        {
            foreach (var service in Dependencies)
            {
                service.OnLoad(plugin);
            }
        }
        
        public void OnMapStart(string mapName)
        {
            foreach (var service in Dependencies)
            {
                service.OnMapStart(mapName);
            }
        }

        public void LoadDependencies(Assembly assembly)
        {
            var typesToAdd = assembly.GetTypes()
                .Where(x => x.IsClass)
                .Where(dependencyType.IsAssignableFrom);

            TypesToAdd.AddRange(typesToAdd);
        }

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
    }
}