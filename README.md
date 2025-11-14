# ModifiedItemDrop

ModifiedItemDrop 是一个面向 Unturned RocketMod 服务器的掉落控制插件，支持分区掉落、衣物容器规则、调试日志以及热重载。

## 功能特性
- **分区概率**：主武器、副武器、普通背包页可分别设置掉落概率。
- **衣物 / 容器**：上衣、裤子、背包、背心、帽子、面罩、眼镜等槽位支持 MatchSlot / UseContentsChance / Preserve 三种模式。
- **自定义物品**：对指定 ItemID 定义优先级最高的掉落概率。
- **调试日志**：`EnableDebugLogging` 打开后输出 `[ModifiedItemDrop::Debug]`，包含概率来源、随机数与判定结果。
- **热重载**：`/mid reload` 即可在不停机的情况下重新加载配置。
- **安全启动**：若 `Lose_Clothes_PvE/PvP` 未关闭，插件会拒绝加载以避免与原生逻辑冲突。

## 环境依赖
- Unturned Dedicated Server + RocketMod
- .NET Framework 4.8 SDK（开发 / 本地构建）

## 构建与部署
1. 运行 `dotnet build -c Release`。
2. 将 `bin/Release/net48/ModifiedItemDrop.dll` 复制至服务器 `Rocket/Plugins/`。
3. 编辑 `Rocket/Plugins/ModifiedItemDrop/ModifiedItemDrop.configuration.xml`。
4. 确保 `Servers/<Name>/Config.json` 中 `Lose_Clothes_PvE=false` 且 `Lose_Clothes_PvP=false`。
5. 启动服务器或在游戏内执行 `/mid reload`。

## 配置说明
所有概率字段均在 [0,1] 区间，示例：

```xml
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
      <!-- 其它槽位可按需添加 -->
    </ClothingRules>
  </RuleSet>
</ModifiedItemDropConfiguration>
```

## 命令与权限
- `/mid reload`：热重载配置（权限 `modifieditemdrop.reload`）。

## 项目结构
```text
ModifiedItemDrop/
- Configuration/    # 配置对象与加载器
- Drop/             # 掉落逻辑（DropService, ChanceResolver）
- Extensions/       # UnturnedPlayer 快照 / 衣物容器辅助
- Models/           # 库存、衣物快照模型
- Plugin/           # 插件入口、事件、命令
- ModifiedItemDrop.configuration.xml
- give.txt          # 常用 /give 测试脚本
- README.md
```

## 开发与测试
- 调参时打开 `EnableDebugLogging` 观察日志。
- 结合 `/give` + `/kill` 快速验证不同槽位（示例见 `give.txt`）。
- 测试衣物逻辑前务必关闭 `Lose_Clothes_PvE/PvP`。
- 修改代码后运行 `dotnet build` 或 `dotnet build -c Release`。

## 发布流程
1. 更新配置 / README / 版本信息。
2. `dotnet build -c Release`。
3. 打包 `bin/Release/net48/ModifiedItemDrop.dll` 及示例配置、变更说明。
4. 上传到 GitHub Releases 或直接替换服务器插件。

---

## English Summary
ModifiedItemDrop brings configurable death/drop behavior to Unturned RocketMod servers. It evaluates every inventory and clothing item on death using the XML rules, drops the items that pass the roll, and restores the rest on revive.

- Partitioned drop chances for primary/secondary weapons and inventory pages.
- Clothing-slot rules supporting MatchSlot / UseContentsChance / Preserve.
- ItemID overrides with highest priority.
- `/mid reload` hot reload plus verbose debug logging.
- Requires `Lose_Clothes_PvE/PvP` to be disabled before loading.

Build & install: `dotnet build -c Release`, copy `bin/Release/net48/ModifiedItemDrop.dll` into `Rocket/Plugins/`, edit the XML config, verify the Lose_Clothes flags are false, then restart or `/mid reload`. Configuration, commands, repository layout, development tips, and release steps are identical to the Chinese sections above.

