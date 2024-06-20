using CounterStrikeSharp.API.Core;

namespace GameModeManager
{
    public interface IPluginDependency<TPlugin, TConfig>
    {
        public virtual void OnConfigParsed(TConfig config) { }
        public virtual void OnMapStart(string mapName) { }
        public virtual void OnLoad(TPlugin plugin) { }
        public virtual void Clear() { }
    }
}