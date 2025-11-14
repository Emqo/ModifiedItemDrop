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
            var serverDropsClothing = ShouldServerDropClothes(player);

            try
            {
                ForceUnequipCurrentItem(player);
                ProcessInventory(player, pending, deathPosition);
                if (serverDropsClothing)
                {
                    DebugLog("Skipping clothing processing because server config already drops clothes on death.");
                }
                else
                {
                    ProcessClothing(player, pending, deathPosition);
                }

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
                RestoreClothing(player, pending);
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

        private void ProcessClothing(UnturnedPlayer player, PendingRestore pending, Vector3 deathPosition)
        {
            var snapshots = player.CaptureClothing();
            if (snapshots.Count == 0)
            {
                return;
            }

            var clothing = player.Player.clothing;
            foreach (var snapshot in snapshots)
            {
                var rule = _configurationLoader.CurrentRuleSet.ResolveClothingRule(snapshot.SlotType);
                var chance = rule.SlotDropChance;
                var roll = _random.NextDouble();
                var shouldDrop = roll <= chance;

                var container = PlayerExtensions.GetClothingContainer(clothing, snapshot.SlotType);
                HandleClothingContents(snapshot, rule, pending, deathPosition, shouldDrop, chance, container);

                var updatedSnapshot = PlayerExtensions.CaptureClothingSlot(clothing, snapshot.SlotType);
                if (updatedSnapshot == null)
                {
                    continue;
                }

                if (shouldDrop)
                {
                    ClearClothingSlot(clothing, snapshot.SlotType);
                    DropWorldItem(updatedSnapshot.Item, deathPosition);
                    DebugLog($"Drop clothing slot={snapshot.SlotType} item={updatedSnapshot.Item.id} chance={chance:F4} roll={roll:F4} -> dropped");
                }
                else
                {
                    pending.ClothingItems.Add(updatedSnapshot);
                    ClearClothingSlot(clothing, snapshot.SlotType);
                    DebugLog($"Keep clothing slot={snapshot.SlotType} item={updatedSnapshot.Item.id} chance={chance:F4} roll={roll:F4} -> kept");
                }
            }
        }

        private static void ClearClothingSlot(PlayerClothing clothing, SlotType slot)
        {
            var emptyState = Array.Empty<byte>();
            switch (slot)
            {
                case SlotType.Shirt:
                    clothing.ReceiveWearShirt(Guid.Empty, 0, emptyState, false);
                    break;
                case SlotType.Pants:
                    clothing.ReceiveWearPants(Guid.Empty, 0, emptyState, false);
                    break;
                case SlotType.Backpack:
                    clothing.ReceiveWearBackpack(Guid.Empty, 0, emptyState, false);
                    break;
                case SlotType.Vest:
                    clothing.ReceiveWearVest(Guid.Empty, 0, emptyState, false);
                    break;
                case SlotType.Hat:
                    clothing.ReceiveWearHat(Guid.Empty, 0, emptyState, false);
                    break;
                case SlotType.Mask:
                    clothing.ReceiveWearMask(Guid.Empty, 0, emptyState, false);
                    break;
                case SlotType.Glasses:
                    clothing.ReceiveWearGlasses(Guid.Empty, 0, emptyState, false);
                    break;
            }
        }

        private void HandleClothingContents(ClothingItemSnapshot snapshot, ClothingSlotRule rule, PendingRestore pending, Vector3 deathPosition, bool clothingWillDrop, double slotChance, Items container)
        {
            if (snapshot.Contents == null || snapshot.Contents.Count == 0)
            {
                return;
            }

            var ordered = new List<ClothingContentSnapshot>(snapshot.Contents);
            ordered.Sort((a, b) => b.Index.CompareTo(a.Index));

            foreach (var content in ordered)
            {
                var item = content.Item;
                if (item == null || item.id == 0)
                {
                    continue;
                }

                double effectiveChance;
                switch (rule.ContentsDropMode)
                {
                    case ClothingContentsDropMode.MatchSlot:
                        effectiveChance = slotChance;
                        break;
                    case ClothingContentsDropMode.UseContentsChance:
                        effectiveChance = rule.ContentsDropChance;
                        break;
                    case ClothingContentsDropMode.Preserve:
                        effectiveChance = 0d;
                        break;
                    default:
                        effectiveChance = slotChance;
                        break;
                }

                var roll = _random.NextDouble();
                var shouldDrop = effectiveChance > 0 && roll <= effectiveChance;

                if (shouldDrop)
                {
                    container?.removeItem(content.Index);
                    DropWorldItem(item, deathPosition);
                    DebugLog($"  Drop contents item={item.id} mode={rule.ContentsDropMode} chance={effectiveChance:F4} roll={roll:F4} -> dropped");
                    continue;
                }

                if (clothingWillDrop)
                {
                    container?.removeItem(content.Index);
                    pending.InventoryItems.Add(new InventoryRestoreRecord(CloneItem(item)));
                    DebugLog($"  Keep contents item={item.id} mode={rule.ContentsDropMode} -> moved to pending restore (clothing dropped)");
                }
                else
                {
                    DebugLog($"  Keep contents item={item.id} mode={rule.ContentsDropMode} -> retained in clothing");
                }
            }
        }

        private void WearClothingItem(PlayerClothing clothing, ClothingItemSnapshot snapshot)
        {
            var state = snapshot.Item.state ?? Array.Empty<byte>();
            switch (snapshot.SlotType)
            {
                case SlotType.Shirt:
                    clothing.askWearShirt(snapshot.Item.id, snapshot.Item.quality, state, true);
                    break;
                case SlotType.Pants:
                    clothing.askWearPants(snapshot.Item.id, snapshot.Item.quality, state, true);
                    break;
                case SlotType.Backpack:
                    clothing.askWearBackpack(snapshot.Item.id, snapshot.Item.quality, state, true);
                    break;
                case SlotType.Vest:
                    clothing.askWearVest(snapshot.Item.id, snapshot.Item.quality, state, true);
                    break;
                case SlotType.Hat:
                    clothing.askWearHat(snapshot.Item.id, snapshot.Item.quality, state, true);
                    break;
                case SlotType.Mask:
                    clothing.askWearMask(snapshot.Item.id, snapshot.Item.quality, state, true);
                    break;
                case SlotType.Glasses:
                    clothing.askWearGlasses(snapshot.Item.id, snapshot.Item.quality, state, true);
                    break;
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

        private void RestoreClothing(UnturnedPlayer player, PendingRestore pending)
        {
            var clothing = player.Player?.clothing;
            if (clothing == null)
            {
                return;
            }

            foreach (var snapshot in pending.ClothingItems)
            {
                WearClothingItem(clothing, snapshot);
            }
        }

        private void RestoreImmediately(UnturnedPlayer player, PendingRestore pending)
        {
            try
            {
                RestoreInventory(player, pending);
                RestoreClothing(player, pending);
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

        private static bool ShouldServerDropClothes(UnturnedPlayer player)
        {
            var life = player?.Player?.life;
            var modeConfig = Provider.modeConfigData;
            if (life == null || modeConfig?.Players == null)
            {
                return false;
            }

            return life.wasPvPDeath ? modeConfig.Players.Lose_Clothes_PvP : modeConfig.Players.Lose_Clothes_PvE;
        }

        private static void ForceUnequipCurrentItem(UnturnedPlayer player)
        {
            var equipment = player?.Player?.equipment;
            if (equipment == null)
            {
                return;
            }

            try
            {
                equipment.dequip();
            }
            catch (Exception)
            {
                // Ignore and continue; equipment might already be unequipped.
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

            public List<ClothingItemSnapshot> ClothingItems { get; } = new List<ClothingItemSnapshot>();

            public bool IsEmpty => InventoryItems.Count == 0 && ClothingItems.Count == 0;
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

