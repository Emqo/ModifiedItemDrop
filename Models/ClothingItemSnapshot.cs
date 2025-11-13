using SDG.Unturned;

namespace FFEmqo.ModifiedItemDrop.Models
{
    public sealed class ClothingItemSnapshot
    {
        public ClothingItemSnapshot(SlotType slotType, Item item)
        {
            SlotType = slotType;
            Item = item;
        }

        public SlotType SlotType { get; }

        public Item Item { get; }
    }
}

