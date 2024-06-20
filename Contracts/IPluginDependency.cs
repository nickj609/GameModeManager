using CounterStrikeSharp.API.Core;

namespace GameModeManager
{
    public interface IPluginDependency<TPlugin, TConfig>
    {
        public virtual void OnConfigParsed(TConfig config) { }
        public virtual void OnLoad(TPlugin plugin) { }
    }
}