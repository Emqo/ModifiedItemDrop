using System;
using System.Collections.Generic;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace FFEmqo.ModifiedItemDrop.Plugin
{
    public sealed class ReloadConfigCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "mid";

        public string Help => "ModifiedItemDrop command suite.";

        public string Syntax => "<reload>";

        public List<string> Aliases => new List<string> { "modifieditemdrop" };

        public List<string> Permissions => new List<string> { "modifieditemdrop.reload" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command == null || command.Length == 0)
            {
                SendUsage(caller);
                return;
            }

            if (!command[0].Equals("reload", StringComparison.OrdinalIgnoreCase))
            {
                SendUsage(caller);
                return;
            }

            var plugin = ModifiedItemDropPlugin.Instance;
            if (plugin == null)
            {
                SendMessage(caller, "Plugin not initialised.", Color.red);
                return;
            }

            if (plugin.TryReloadConfiguration(out var error))
            {
                SendMessage(caller, "ModifiedItemDrop configuration reloaded.", Color.green);
            }
            else
            {
                SendMessage(caller, $"Reload failed: {error}", Color.red);
            }
        }

        private static void SendUsage(IRocketPlayer caller)
        {
            SendMessage(caller, "Usage: /mid reload", Color.yellow);
        }

        private static void SendMessage(IRocketPlayer caller, string message, Color color)
        {
            if (caller is UnturnedPlayer player)
            {
                UnturnedChat.Say(player, message, color);
                return;
            }

            if (caller == null)
            {
                Logger.Log(message);
                return;
            }

            Logger.Log(message);
        }
    }
}

