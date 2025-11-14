# ModifiedItemDrop

ModifiedItemDrop adds fine-grained death/drop rules to Unturned RocketMod servers. It supports region-based probabilities, clothing container rules, per-item overrides, verbose debug logging, and hot reloads.

## Overview
The plugin listens to UnturnedPlayerEvents.OnPlayerDeath, snapshots the player inventory/clothing, evaluates each item against the XML configuration, drops items that pass the roll and restores the rest on revive. It also refuses to load when the server still has Lose_Clothes_PvE/PvP enabled to avoid double-dropping.

## Features
- Partitioned drop chances for primary weapon, secondary weapon, and general inventory pages.
- Clothing slot rules (shirt, pants, backpack, vest, hat, mask, glasses) with independent slot/contents logic.
- Custom item overrides by ItemID.
- Debug logging that prints the random roll, probability source, and result.
- /mid reload hot reload command.
- Safety guard that enforces Lose_Clothes = false before loading.

## Requirements
- Unturned Dedicated Server + RocketMod
- .NET Framework 4.8 SDK (for building)

## Build & Install
1. dotnet build -c Release
2. Copy bin/Release/net48/ModifiedItemDrop.dll to Rocket/Plugins/.
3. Edit Rocket/Plugins/ModifiedItemDrop/ModifiedItemDrop.configuration.xml.
4. Ensure Servers/<Name>/Config.json sets Lose_Clothes_PvE=false and Lose_Clothes_PvP=false.
5. Start the server or run /mid reload.

## Configuration
All probabilities are in [0,1]. Example structure:

~~~xml
<ModifiedItemDropConfiguration>
  <EnableDebugLogging>false</EnableDebugLogging>
  <RuleSet>
    <GlobalDefaultChance>0.5</GlobalDefaultChance>
    <RegionChances>
      <RegionChanceEntry><Region>PrimaryWeapon</Region><Chance>0.7</Chance></RegionChanceEntry>
      <RegionChanceEntry><Region>SecondaryWeapon</Region><Chance>0.3</Chance></RegionChanceEntry>
      <RegionChanceEntry><Region>Inventory</Region><Chance>0.5</Chance></RegionChanceEntry>
    </RegionChances>
    <CustomItemChances>
      <ItemChanceEntry><ItemID>81</ItemID><Chance>0.2</Chance></ItemChanceEntry>
    </CustomItemChances>
    <ClothingRules>
      <ClothingSlot>
        <Slot>Backpack</Slot>
        <SlotDropChance>0.25</SlotDropChance>
        <ContentsDropMode>UseContentsChance</ContentsDropMode>
        <ContentsDropChance>0.5</ContentsDropChance>
      </ClothingSlot>
      <!-- ...other slots... -->
    </ClothingRules>
  </RuleSet>
</ModifiedItemDropConfiguration>
~~~

## Commands & Permissions
- /mid reload - reload configuration (permission modifieditemdrop.reload)

## Repository Layout
~~~text
ModifiedItemDrop/
- Configuration/    # configuration objects & loader
- Drop/             # core drop logic (DropService, ChanceResolver)
- Extensions/       # UnturnedPlayer snapshot helpers
- Models/           # inventory & clothing snapshot models
- Plugin/           # Rocket plugin entry, events, commands
- ModifiedItemDrop.configuration.xml
- give.txt          # convenience /give script for testing
- README.md
~~~

## Development & Testing
- Toggle EnableDebugLogging while tuning probabilities.
- Use /give + /kill to exercise different slots (see give.txt).
- Always disable the vanilla Lose_Clothes flags before testing clothing rules.
- Run dotnet build (debug) or dotnet build -c Release (release) and check for compiler errors.

## Release Process
1. Update configuration/README/version info.
2. dotnet build -c Release
3. Package bin/Release/net48/ModifiedItemDrop.dll plus the sample config/changelog.
4. Publish to GitHub Releases or deploy directly to the server.
