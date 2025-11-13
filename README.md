<<<<<<< HEAD
# ModifiedItemDrop

## 快速上手

### 构建插件
1. 在 Visual Studio 中按 `Ctrl + Shift + B`（或菜单 `Build -> Build Solution`）完成编译。
2. 将 `bin/Debug/net48/ModifiedItemDrop.dll` 复制到 Unturned 服务器的 `Rocket/Plugins` 目录。

### 基础测试
启动服务器，观察控制台：
```
[loading] ModifiedItemDrop
[05/16/2024 06:30:26] [Info] ModifiedItemDrop >> ModifiedItemDrop 1.0.0 has been loaded!
```
若启用了调试开关 `EnableDebugLogging`，死亡判定时会输出概率日志。

### 配置重载
修改 `ModifiedItemDrop.configuration.xml` 后，可在游戏内执行 `/mid reload` 快速应用变更。

## 插件设计文档

## 概述
- **插件名称**：`ModifiedItemDrop`
- **目标平台**：Unturned + RocketMod
- **设计目标**：提供对玩家死亡或离线时物品掉落的精细化控制，支持按衣物部位、容器区域、主副手武器以及单独物品配置掉落概率，并允许调试与热重载。

## 功能规划
- **衣物掉落控制**：为头盔、面罩、护目镜、上衣、裤子、背包、战术背心等部位配置独立掉落概率。
- **容器掉落控制**：按容器类型（如上衣兜、裤兜、背包、背心等）设置基础掉落概率，可进一步细化到指定槽位。
- **武器绑位控制**：主手、副手、背部（初级/二级）分别配置掉落概率，支持是否保留弹匣和附件。
- **物品覆盖规则**：针对特定物品 ID 或标签定义覆盖概率及附件保留策略，优先级最高。
- **配置与热重载**：使用 Rocket 配置系统 XML/JSON，支持运行期 `/mid reload` 重新加载。
- **命令与权限**：提供重载与预览命令，配套权限节点方便管理员使用。
- **日志调试**：支持调试日志输出，显示每次掉落判定的详细数据。

## 核心场景
1. **玩家死亡**
   - 汇总当前装备、容器物品、武器绑位，按配置计算掉落或保留。
   - 根据随机判定结果移除或保留物品，并处理附件与弹匣。
2. **玩家断线/离线**（可选）
   - 根据服务器需求，决定断线是否触发掉落逻辑。
3. **管理员操作**
   - `/mid reload`：重载配置。
   - `/mid preview <player>`：输出指定玩家的掉落配置预览。
   - `/mid set <scope> <value>`：临时调整概率或规则（可选拓展）。

## 配置结构示例
```yaml
# Rocket/Plugins/ModifiedItemDrop/config.yml
general:
  default_drop_chance: 0.5
  round_mode: floor
  enable_debug_log: false

binds:
  main_hand:
    drop_chance: 0.3
    preserve_attachments: true
  off_hand:
    drop_chance: 0.5
  back_primary:
    drop_chance: 0.25
    preserve_magazine: true

clothing:
  helmet: 0.1
  mask: 0.2
  shirt: 0.4
  pants: 0.35
  backpack: 0.5
  vest: 0.3

containers:
  shirt:
    base_drop: 0.35
    per_slot:
      slot_0: 0.5
      slot_1: 0.2
  pants:
    base_drop: 0.3
  backpack:
    base_drop: 0.4
    allow_overwrite_by_tag: true

item_overrides:
  id:363:
    drop_chance: 0.6
    preserve_magazine: false
    preserve_attachments: true
  tag:medkit:
    drop_chance: 0.1
```

## 技术实现要点
- **事件钩子**：通过 `UnturnedPlayerEvents.OnPlayerDeath`、`U.Events.OnPlayerDisconnected` 捕获死亡与离线事件；结合 `PlayerClothing`、`PlayerInventory`、`PlayerEquipment` 获取物品。
- **概率决策**：封装 `DropEvaluator`，按优先级 `ItemOverride` > `Slot` > `Container` > `Bind` > `Clothing` > `Default` 解析概率，并通过注入式 `RandomProvider` 实际抽样。
- **配置缓存**：`DropConfigurationCache` 将配置转换为字典查找，确保判定时为 O(1) 查询。
- **可测试性**：将随机源与配置缓存解耦，便于编写单元测试模拟不同概率场景。

## 权限与命令
- `modifieditemdrop.reload`：允许执行 `/mid reload`。
- `modifieditemdrop.preview`：允许执行 `/mid preview`。
- `modifieditemdrop.set`：允许执行 `/mid set`（如实现）。
- 在 `RocketPermissions.config.xml` 中配置对应权限分组。

## 测试计划
- **单元测试**：覆盖配置解析、概率优先级、随机判定边界值（0、1、极端配置）。
- **集成测试**：在测试服务器上模拟玩家死亡/断线场景，验证衣物、绑定槽位与容器物品的掉落行为；检查热重载是否即时生效。
- **回归测试**：确保未配置项遵循默认概率；核对调试日志输出正确。

## 项目结构
```
ModifiedItemDrop/
  |-- ModifiedItemDrop.slnx
  |-- ModifiedItemDrop.csproj
  |-- ModifiedItemDropPlugin.cs
  |-- ModifiedItemDropConfiguration.cs
  |-- Configuration/
      |-- GeneralSettings.cs
      |-- BindRule.cs
      |-- ContainerRule.cs
      |-- SlotDropRule.cs
      |-- ItemOverrideRule.cs
      |-- ProbabilityEntry.cs
  |-- Models/
      |-- DropItemContext.cs
      |-- DropDecision.cs
  |-- Services/
      |-- DropConfigurationCache.cs
      |-- DropEvaluator.cs
      |-- RandomProvider.cs
  |-- Helpers/
      |-- InventoryHelper.cs
  |-- Commands/
      |-- ReloadCommand.cs (待实现)
      |-- PreviewCommand.cs (待实现)
  |-- README.md
  |-- Project.md
```

## 后续迭代
- 扩展 `InventoryHelper`：支持容器、绑定槽位、物品标签采集。
- 实装掉落动作：根据决策移除物品、生成掉落物，并处理附件/弹匣保留。
- 完成命令实现与权限校验，支持运行时预览与临时调整。
- 增加断线掉落策略、调试日志级别和开关。
- 编写自动化测试和示例配置，完善 README 使用说明。
