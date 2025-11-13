using System;
using FFEmqo.ModifiedItemDrop.Drop;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using Logger = Rocket.Core.Logging.Logger;

namespace FFEmqo.ModifiedItemDrop.Plugin
{
    public sealed class PlayerDeathHandler
    {
        private readonly DropService _dropService;
        private bool _isEnabled;

        public PlayerDeathHandler(DropService dropService)
        {
            _dropService = dropService ?? throw new ArgumentNullException(nameof(dropService));
        }

        public void Enable()
        {
            if (_isEnabled)
            {
                return;
            }

            UnturnedPlayerEvents.OnPlayerDeath += OnPlayerDeath;
            UnturnedPlayerEvents.OnPlayerRevive += OnPlayerRevive;
            _isEnabled = true;
        }

        public void Disable()
        {
            if (!_isEnabled)
            {
                return;
            }

            UnturnedPlayerEvents.OnPlayerDeath -= OnPlayerDeath;
            UnturnedPlayerEvents.OnPlayerRevive -= OnPlayerRevive;

            _isEnabled = false;
        }

        private void OnPlayerDeath(UnturnedPlayer player, EDeathCause cause, ELimb limb, CSteamID murderer)
        {
            if (player == null)
            {
                return;
            }

            _dropService.HandlePlayerDying(player);
        }

        private void OnPlayerRevive(UnturnedPlayer player, UnityEngine.Vector3 position, byte angle)
        {
            if (player == null)
            {
                return;
            }

            _dropService.HandlePlayerRevived(player);
        }
    }
}

