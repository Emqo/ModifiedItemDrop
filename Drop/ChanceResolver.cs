using System;
using FFEmqo.ModifiedItemDrop.Configuration;
using FFEmqo.ModifiedItemDrop.Models;

namespace FFEmqo.ModifiedItemDrop.Drop
{
    public sealed class ChanceResolver
    {
        private DropRuleSet _ruleSet;

        public ChanceResolver(DropRuleSet ruleSet)
        {
            UpdateRuleSet(ruleSet);
        }

        public void UpdateRuleSet(DropRuleSet ruleSet)
        {
            _ruleSet = ruleSet ?? DropRuleSet.CreateDefault();
        }

        public double GetChance(SlotType slotType, ushort itemId, out string source)
        {
            if (_ruleSet == null)
            {
                source = "Global";
                return 0d;
            }

            return _ruleSet.ResolveChance(slotType, itemId, out source);
        }
    }
}

