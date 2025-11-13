using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using FFEmqo.ModifiedItemDrop.Models;

namespace FFEmqo.ModifiedItemDrop.Configuration
{
    public class DropRuleSet
    {
        public double GlobalDefaultChance { get; set; } = 0.5d;

        public List<RegionChanceEntry> RegionChances { get; set; } = new List<RegionChanceEntry>();

        public List<ItemChanceEntry> CustomItemChances { get; set; } = new List<ItemChanceEntry>();

        [XmlIgnore]
        private Dictionary<string, double> _regionChanceMap;

        [XmlIgnore]
        private Dictionary<ushort, double> _itemChanceMap;

        public static DropRuleSet CreateDefault()
        {
            return new DropRuleSet
            {
                GlobalDefaultChance = 0.5d,
                RegionChances = new List<RegionChanceEntry>
                {
                    new RegionChanceEntry { Region = nameof(SlotType.PrimaryWeapon), Chance = 0.7d },
                    new RegionChanceEntry { Region = nameof(SlotType.SecondaryWeapon), Chance = 0.3d },
                    new RegionChanceEntry { Region = nameof(SlotType.Shirt), Chance = 0.4d },
                    new RegionChanceEntry { Region = nameof(SlotType.Pants), Chance = 0.4d },
                    new RegionChanceEntry { Region = nameof(SlotType.Backpack), Chance = 0.5d },
                    new RegionChanceEntry { Region = nameof(SlotType.Vest), Chance = 0.6d },
                    new RegionChanceEntry { Region = nameof(SlotType.Hat), Chance = 0.3d },
                    new RegionChanceEntry { Region = nameof(SlotType.Mask), Chance = 0.3d },
                    new RegionChanceEntry { Region = nameof(SlotType.Glasses), Chance = 0.3d },
                    new RegionChanceEntry { Region = nameof(SlotType.Inventory), Chance = 0.5d }
                },
                CustomItemChances = new List<ItemChanceEntry>()
            };
        }

        public DropRuleSet NormalizedCopy()
        {
            var ruleSet = new DropRuleSet
            {
                GlobalDefaultChance = ClampChance(GlobalDefaultChance),
                RegionChances = new List<RegionChanceEntry>(),
                CustomItemChances = new List<ItemChanceEntry>()
            };

            if (RegionChances != null)
            {
                foreach (var entry in RegionChances)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.Region))
                    {
                        continue;
                    }

                    ruleSet.RegionChances.Add(new RegionChanceEntry
                    {
                        Region = entry.Region.Trim(),
                        Chance = ClampChance(entry.Chance)
                    });
                }
            }

            if (CustomItemChances != null)
            {
                foreach (var entry in CustomItemChances)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    ruleSet.CustomItemChances.Add(new ItemChanceEntry
                    {
                        ItemID = entry.ItemID,
                        Chance = ClampChance(entry.Chance)
                    });
                }
            }

            ruleSet.ResetCaches();
            return ruleSet;
        }

        public double ResolveChance(SlotType slotType, ushort itemId, out string source)
        {
            EnsureCaches();

            if (_itemChanceMap.TryGetValue(itemId, out var customChance))
            {
                source = $"Item:{itemId}";
                return customChance;
            }

            var slotKey = slotType.ToString();
            if (_regionChanceMap.TryGetValue(slotKey, out var regionChance))
            {
                source = $"Region:{slotKey}";
                return regionChance;
            }

            source = "Global";
            return ClampChance(GlobalDefaultChance);
        }

        private void EnsureCaches()
        {
            if (_regionChanceMap != null && _itemChanceMap != null)
            {
                return;
            }

            _regionChanceMap = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            if (RegionChances != null)
            {
                foreach (var entry in RegionChances)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.Region))
                    {
                        continue;
                    }

                    _regionChanceMap[entry.Region.Trim()] = ClampChance(entry.Chance);
                }
            }

            _itemChanceMap = new Dictionary<ushort, double>();
            if (CustomItemChances != null)
            {
                foreach (var entry in CustomItemChances)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    _itemChanceMap[entry.ItemID] = ClampChance(entry.Chance);
                }
            }
        }

        private void ResetCaches()
        {
            _regionChanceMap = null;
            _itemChanceMap = null;
        }

        private static double ClampChance(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return 0d;
            }

            if (value < 0d)
            {
                return 0d;
            }

            if (value > 1d)
            {
                return 1d;
            }

            return value;
        }
    }

    public class RegionChanceEntry
    {
        public string Region { get; set; }

        public double Chance { get; set; }
    }

    public class ItemChanceEntry
    {
        public ushort ItemID { get; set; }

        public double Chance { get; set; }
    }
}

