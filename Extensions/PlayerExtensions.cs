using System.Collections.Generic;
using FFEmqo.ModifiedItemDrop.Models;
using Rocket.Unturned.Player;
using SDG.Unturned;

namespace FFEmqo.ModifiedItemDrop.Extensions
{
    public static class PlayerExtensions
    {
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
    }
}

