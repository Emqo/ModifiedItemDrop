using System;
using System.Collections.Generic;
using FFEmqo.ModifiedItemDrop.Configuration;
using FFEmqo.ModifiedItemDrop.Extensions;
using FFEmqo.ModifiedItemDrop.Models;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace FFEmqo.ModifiedItemDrop.Drop
{
    public sealed class DropService
    {
        private readonly ConfigurationLoader _configurationLoader;
        private readonly ChanceResolver _chanceResolver;
        private readonly System.Random _random;
        private readonly Dictionary<CSteamID, PendingRestore> _pendingRestores = new Dictionary<CSteamID, PendingRestore>();

        public DropService(ConfigurationLoader configurationLoader)
        {
            _configurationLoader = configurationLoader ?? throw new ArgumentNullException(nameof(configurationLoader));
            _chanceResolver = new ChanceResolver(configurationLoader.CurrentRuleSet);
            _random = new System.Random();
        }

        public void RefreshRules()
        {
            _chanceResolver.UpdateRuleSet(_configurationLoader.CurrentRuleSet);
        }

        public void HandlePlayerDying(UnturnedPlayer player)
        {
            if (player == null || player.Player == null)
            {
                return;
            }

            DebugLog($"HandlePlayerDying: player={player.CharacterName} ({player.CSteamID}) position={player.Position}");

            RefreshRules();

            var pending = new PendingRestore(player);
            var deathPosition = player.Position;

            try
            {
                ProcessInventory(player, pending, deathPosition);

                if (pending.IsEmpty)
                {
                    _pendingRestores.Remove(player.CSteamID);
                }
                else
                {
                    _pendingRestores[player.CSteamID] = pending;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                // In case of failure, ensure we do not lose items by restoring immediately.
                RestoreImmediately(player, pending);
            }
        }

        public void HandlePlayerRevived(UnturnedPlayer player)
        {
            if (player == null)
            {
                return;
            }

            if (_pendingRestores.TryGetValue(player.CSteamID, out var pending))
            {
                RestoreInventory(player, pending);
                _pendingRestores.Remove(player.CSteamID);
            }
        }

        public void HandlePlayerDisconnected(UnturnedPlayer player)
        {
            if (player == null)
            {
                return;
            }

            if (_pendingRestores.TryGetValue(player.CSteamID, out var pending))
            {
                RestoreImmediately(player, pending);
                _pendingRestores.Remove(player.CSteamID);
            }
        }

        private void ProcessInventory(UnturnedPlayer player, PendingRestore pending, Vector3 deathPosition)
        {
            var inventory = player.Player.inventory;
            if (inventory == null)
            {
                return;
            }

            var snapshots = player.CaptureInventory();
            if (snapshots.Count == 0)
            {
                return;
            }

            // Process per page in reverse order to keep indexes valid.
            var groupedByPage = new Dictionary<byte, List<InventoryItemSnapshot>>();

            foreach (var snapshot in snapshots)
            {
                if (!groupedByPage.TryGetValue(snapshot.Page, out var list))
                {
                    list = new List<InventoryItemSnapshot>();
                    groupedByPage[snapshot.Page] = list;
                }

                list.Add(snapshot);
            }

            foreach (var pair in groupedByPage)
            {
                var page = pair.Key;
                var itemsInPage = pair.Value;
                itemsInPage.Sort((a, b) => b.Index.CompareTo(a.Index));

                foreach (var snapshot in itemsInPage)
                {
                    var jar = snapshot.Jar;
                    if (jar?.item == null || jar.item.id == 0)
                    {
                        continue;
                    }

                    var slotType = GetSlotTypeForPage(page);
                    var chance = _chanceResolver.GetChance(slotType, jar.item.id, out var source);
                    var roll = _random.NextDouble();
                    var shouldDrop = roll <= chance;

                    inventory.removeItem(page, snapshot.Index);

                    if (shouldDrop)
                    {
                        DropWorldItem(jar.item, deathPosition);
                        DebugLog($"Drop item id={jar.item.id} slot={slotType} source={source} roll={roll:F4} <= {chance:F4} -> dropped");
                    }
                    else
                    {
                        pending.InventoryItems.Add(new InventoryRestoreRecord(CloneItem(jar.item)));
                        DebugLog($"Keep item id={jar.item.id} slot={slotType} source={source} roll={roll:F4} > {chance:F4} -> kept");
                    }
                }
            }
        }

        private void RestoreInventory(UnturnedPlayer player, PendingRestore pending)
        {
            var inventory = player.Player?.inventory;
            if (inventory == null)
            {
                return;
            }

            foreach (var record in pending.InventoryItems)
            {
                var item = CloneItem(record.Item);
                inventory.forceAddItem(item, true);
            }
        }

        private void RestoreImmediately(UnturnedPlayer player, PendingRestore pending)
        {
            try
            {
                RestoreInventory(player, pending);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        private static void DropWorldItem(Item item, Vector3 position)
        {
            var clone = CloneItem(item);
            var dropPosition = position + Vector3.up * 0.5f;
            ItemManager.dropItem(clone, dropPosition, false, true, true);
        }

        private static Item CloneItem(Item item)
        {
            if (item == null)
            {
                return null;
            }

            var state = item.state != null ? (byte[])item.state.Clone() : Array.Empty<byte>();
            return new Item(item.id, item.amount, item.quality, state);
        }

        private void DebugLog(string message)
        {
            if (!_configurationLoader.IsDebugLoggingEnabled)
            {
                return;
            }

            Logger.Log($"[ModifiedItemDrop::Debug] {message}");
        }

        private static SlotType GetSlotTypeForPage(byte page)
        {
            switch (page)
            {
                case 0:
                    return SlotType.PrimaryWeapon;
                case 1:
                    return SlotType.SecondaryWeapon;
                default:
                    return SlotType.Inventory;
            }
        }

        private sealed class PendingRestore
        {
            public PendingRestore(UnturnedPlayer player)
            {
                Player = player;
            }

            public UnturnedPlayer Player { get; }

            public List<InventoryRestoreRecord> InventoryItems { get; } = new List<InventoryRestoreRecord>();

            public bool IsEmpty => InventoryItems.Count == 0;
        }

        private sealed class InventoryRestoreRecord
        {
            public InventoryRestoreRecord(Item item)
            {
                Item = item;
            }

            public Item Item { get; }
        }
    }
}

