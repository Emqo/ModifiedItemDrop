using System;
using FFEmqo.ModifiedItemDrop.Configuration;
using FFEmqo.ModifiedItemDrop.Drop;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using System.Reflection;
using SDG.Unturned;

namespace FFEmqo.ModifiedItemDrop.Plugin
{
    public sealed class ModifiedItemDropPlugin : RocketPlugin<ModifiedItemDropConfiguration>
    {
        private PlayerDeathHandler _deathHandler;

        public static ModifiedItemDropPlugin Instance { get; private set; }

        public ConfigurationLoader ConfigurationLoader { get; private set; }

        public DropService DropService { get; private set; }

        protected override void Load()
        {
            if (Instance != null)
            {
                throw new InvalidOperationException("Attempted to load plugin twice.");
            }

            Instance = this;
            EnsureServerClothingDropsDisabled();

            ConfigurationLoader = new ConfigurationLoader(this);
            DropService = new DropService(ConfigurationLoader);
            _deathHandler = new PlayerDeathHandler(DropService);
            _deathHandler.Enable();

            Logger.Log($"{Name} {Assembly.GetName().Version.ToString(3)} has been loaded!");
        }

        protected override void Unload()
        {
            _deathHandler?.Disable();
            _deathHandler = null;
            DropService = null;
            ConfigurationLoader = null;
            Instance = null;

            Logger.Log($"{Name} has been unloaded!");
        }

        public bool TryReloadConfiguration(out string error)
        {
            if (ConfigurationLoader == null)
            {
                error = "Configuration loader not ready.";
                return false;
            }

            var result = ConfigurationLoader.TryReload(out error);
            if (result)
            {
                DropService?.RefreshRules();
            }

            return result;
        }

        private static void EnsureServerClothingDropsDisabled()
        {
            var players = Provider.modeConfigData?.Players;
            if (players == null)
            {
                Logger.LogWarning("[ModifiedItemDrop] Unable to verify Lose_Clothes settings; modeConfigData.Players is null.");
                return;
            }

            if (players.Lose_Clothes_PvE || players.Lose_Clothes_PvP)
            {
                const string message = "[ModifiedItemDrop] ServerConfig: Players.Lose_Clothes_PvE/PvP must be false for ModifiedItemDrop to run. Please update Servers/Default/Config.json and restart.";
                Logger.LogError(message);
                throw new InvalidOperationException(message);
            }
        }
    }
}

