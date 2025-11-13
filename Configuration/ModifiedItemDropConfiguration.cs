using Rocket.API;

namespace FFEmqo.ModifiedItemDrop.Configuration
{
    public class ModifiedItemDropConfiguration : IRocketPluginConfiguration
    {
        public bool EnableDebugLogging { get; set; } = false;

        public DropRuleSet RuleSet { get; set; } = DropRuleSet.CreateDefault();

        public void LoadDefaults()
        {
            EnableDebugLogging = false;
            RuleSet = DropRuleSet.CreateDefault();
        }
    }
}

