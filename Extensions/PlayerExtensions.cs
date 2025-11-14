using System;
using System.Collections.Generic;
using System.Reflection;
using FFEmqo.ModifiedItemDrop.Models;
using Rocket.Unturned.Player;
using SDG.Unturned;

namespace FFEmqo.ModifiedItemDrop.Extensions
{
    public static class PlayerExtensions
    {
        private static readonly SlotType[] ClothingSlots =
        {
            SlotType.Shirt,
            SlotType.Pants,
            SlotType.Backpack,
            SlotType.Vest,
            SlotType.Hat,
            SlotType.Mask,
            SlotType.Glasses
        };

        private static readonly FieldInfo ShirtItemsField = typeof(PlayerClothing).GetField("shirtItems", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo PantsItemsField = typeof(PlayerClothing).GetField("pantsItems", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo BackpackItemsField = typeof(PlayerClothing).GetField("backpackItems", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo VestItemsField = typeof(PlayerClothing).GetField("vestItems", BindingFlags.Instance | BindingFlags.NonPublic);

        public static List<InventoryItemSnapshot> CaptureInventory(this UnturnedPlayer player)
        {
            var snapshots = new List<InventoryItemSnapshot>();

            if (player?.Player?.inventory == null)
            {
                return snapshots;
            }

            var inventory = player.Player.inventory;
            for (byte page = 0; page < PlayerInventory.PAGES; page++)
            {
                if (inventory.items == null || page >= inventory.items.Length)
                {
                    continue;
                }

                var pageItems = inventory.items[page];
                if (pageItems == null)
                {
                    continue;
                }

                var count = pageItems.getItemCount();
                for (byte index = 0; index < count; index++)
                {
                    var jar = pageItems.getItem(index);
                    if (jar != null)
                    {
                        snapshots.Add(new InventoryItemSnapshot(page, index, jar));
                    }
                }
            }

            return snapshots;
        }

        public static List<ClothingItemSnapshot> CaptureClothing(this UnturnedPlayer player)
        {
            var snapshots = new List<ClothingItemSnapshot>();
            var clothing = player?.Player?.clothing;
            if (clothing == null)
            {
                return snapshots;
            }

            foreach (var slot in ClothingSlots)
            {
                var snapshot = CaptureClothingSlot(clothing, slot);
                if (snapshot != null)
                {
                    snapshots.Add(snapshot);
                }
            }

            return snapshots;
        }

        internal static ClothingItemSnapshot CaptureClothingSlot(PlayerClothing clothing, SlotType slot)
        {
            var item = CloneClothingItem(clothing, slot);
            if (item == null || item.id == 0)
            {
                return null;
            }

            var container = GetClothingContainer(clothing, slot);
            var contents = CaptureClothingContents(container);
            return new ClothingItemSnapshot(slot, item, contents);
        }

        internal static Items GetClothingContainer(PlayerClothing clothing, SlotType slot)
        {
            if (clothing == null)
            {
                return null;
            }

            switch (slot)
            {
                case SlotType.Shirt:
                    return ShirtItemsField?.GetValue(clothing) as Items;
                case SlotType.Pants:
                    return PantsItemsField?.GetValue(clothing) as Items;
                case SlotType.Backpack:
                    return BackpackItemsField?.GetValue(clothing) as Items;
                case SlotType.Vest:
                    return VestItemsField?.GetValue(clothing) as Items;
                default:
                    return null;
            }
        }

        internal static List<ClothingContentSnapshot> CaptureClothingContents(Items container)
        {
            var contents = new List<ClothingContentSnapshot>();

            if (container == null)
            {
                return contents;
            }

            var count = container.getItemCount();
            for (byte index = 0; index < count; index++)
            {
                var jar = container.getItem(index);
                if (jar?.item != null)
                {
                    contents.Add(new ClothingContentSnapshot(index, CloneItem(jar.item)));
                }
            }

            return contents;
        }

        private static Item CloneClothingItem(PlayerClothing clothing, SlotType slot)
        {
            ushort id;
            byte quality;
            byte[] state;

            switch (slot)
            {
                case SlotType.Shirt:
                    id = clothing.shirt;
                    quality = clothing.shirtQuality;
                    state = clothing.shirtState;
                    break;
                case SlotType.Pants:
                    id = clothing.pants;
                    quality = clothing.pantsQuality;
                    state = clothing.pantsState;
                    break;
                case SlotType.Backpack:
                    id = clothing.backpack;
                    quality = clothing.backpackQuality;
                    state = clothing.backpackState;
                    break;
                case SlotType.Vest:
                    id = clothing.vest;
                    quality = clothing.vestQuality;
                    state = clothing.vestState;
                    break;
                case SlotType.Hat:
                    id = clothing.hat;
                    quality = clothing.hatQuality;
                    state = clothing.hatState;
                    break;
                case SlotType.Mask:
                    id = clothing.mask;
                    quality = clothing.maskQuality;
                    state = clothing.maskState;
                    break;
                case SlotType.Glasses:
                    id = clothing.glasses;
                    quality = clothing.glassesQuality;
                    state = clothing.glassesState;
                    break;
                default:
                    return null;
            }

            if (id == 0)
            {
                return null;
            }

            var stateCopy = state != null ? (byte[])state.Clone() : Array.Empty<byte>();
            return new Item(id, 1, quality, stateCopy);
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
    }
}
