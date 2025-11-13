using System;
using FFEmqo.ModifiedItemDrop.Plugin;
using Rocket.Core.Logging;

namespace FFEmqo.ModifiedItemDrop.Configuration
{
    public sealed class ConfigurationLoader
    {
        private DropRuleSet _currentRuleSet;
        private readonly ModifiedItemDropPlugin _plugin;

        public bool IsDebugLoggingEnabled { get; private set; }

        public ConfigurationLoader(ModifiedItemDropPlugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            ReloadFromConfiguration();
        }

        public DropRuleSet CurrentRuleSet => _currentRuleSet;

        public void ReloadFromConfiguration()
        {
            var config = _plugin.Configuration?.Instance ?? new ModifiedItemDropConfiguration();
            _currentRuleSet = config.RuleSet?.NormalizedCopy() ?? DropRuleSet.CreateDefault();
            IsDebugLoggingEnabled = config.EnableDebugLogging;
        }

        public bool TryReload(out string error)
        {
            try
            {
                ReloadFromConfiguration();
                Logger.Log("ModifiedItemDrop configuration reloaded successfully.");
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                error = ex.Message;
                return false;
            }
        }
    }
}

