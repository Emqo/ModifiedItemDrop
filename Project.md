# ModifiedItemDrop

## 一、项目简介

**ModifiedItemDrop** 是一个专为 Unturned 服务器设计的 RocketMod 插件，用于**完全替代原版玩家死亡掉落机制**。
 插件允许服务器管理员通过配置文件自定义玩家各个物品栏与装备槽的掉落概率，包括上衣、裤子、背包、帽子、主武器、副武器等。

与传统全局掉落概率不同，本插件可以实现**分区域、分装备槽**、甚至**分物品 ID** 的精确掉落控制。
 插件在玩家死亡前进行判定，依据配置计算每个物品是否应掉落，命中则掉落在死亡位置，未命中则默认保留。

------

## 二、主要功能

- **完全替代原版玩家死亡掉落机制**
- **可独立控制各个背包区域掉落率**（衣物、主副武器、装备栏、储物栏）
- **支持单个物品 ID 定义专属掉落概率**
- **支持主手、副手、背包物品区分掉落**
- **支持全局默认掉落率作为兜底**
- **支持命令热重载配置文件**
- **轻量执行，不影响服务器性能**

------

## 三、核心逻辑

1. 监听 `UnturnedPlayerEvents.OnPlayerDying`。
2. 获取玩家装备信息与背包内容：
   - 主武器、副武器
   - 各衣物槽（上衣、裤子、背包、帽子、面具、护目镜）
   - 各衣物内部的储物格（`Clothing` 容器）
   - 主背包与快捷栏物品
3. 根据配置文件的概率规则，为每个槽位或物品生成一个随机数。
4. 命中则执行 `DropItem(item, position)` 掉落物品。
5. 未命中则忽略，不掉落。
6. 阻止游戏原生掉落逻辑（通过事件处理优先级或自定义清空）。

------

## 四、配置文件设计

配置文件：`ModifiedItemDrop.config.json`

### 示例

```
{
  "GlobalDefaultChance": 0.5,
  "RegionChances": {
    "PrimaryWeapon": 0.7,
    "SecondaryWeapon": 0.3,
    "Shirt": 0.4,
    "Pants": 0.4,
    "Backpack": 0.5,
    "Vest": 0.6,
    "Hat": 0.3,
    "Mask": 0.3,
    "Glasses": 0.3,
    "Inventory": 0.5
  },
  "CustomItemChances": {
    "15": 0.9,
    "81": 0.2,
    "363": 0.05
  }
}
```

### 字段说明

- **GlobalDefaultChance**
   未指定区域或物品时的全局默认掉落概率。
- **RegionChances**
   各个装备区域或槽位的基础掉落概率。
   若某区域内的物品未定义专属概率，则使用该区域的概率。
- **CustomItemChances**
   以物品 ID 为键的独立概率映射（优先级最高，避免重复项）。

------

## 五、概率判定优先级

插件判定物品掉落概率时，按以下优先级生效：

```
若配置了 CustomItemChances → 使用该物品的概率  
否则若所在区域在 RegionChances 中 → 使用该区域的概率  
否则使用 GlobalDefaultChance  
```

------

## 六、命令与权限

**命令**

- `/mid reload`
  - 功能：热重载配置文件
  - 权限：`modifieditemdrop.reload`

**权限节点**

- `modifieditemdrop.admin`：管理权限
- `modifieditemdrop.reload`：允许重载配置

------

## 七、项目结构建议

```
ModifiedItemDrop/
├── ModifiedItemDrop.csproj                 # 插件主项目文件
├── Plugin/
│   ├── ModifiedItemDropPlugin.cs           # Rocket 插件入口（注册命令 / 事件）
│   ├── PlayerDeathHandler.cs               # 玩家死亡事件处理器
│   └── ReloadConfigCommand.cs              # `/mid reload` 命令实现
├── Configuration/
│   ├── ModifiedItemDropConfiguration.cs    # Rocket 配置实体与默认值
│   └── ConfigurationLoader.cs              # 加载、校验与热重载逻辑
├── Drop/
│   ├── DropService.cs                      # 统一入口，协调概率判定与掉落执行
│   ├── ChanceResolver.cs                   # 概率优先级判定（自定义 → 区域 → 全局）
│   └── SlotDropRule.cs                     # 单个槽位的掉落规则描述
├── Models/
│   ├── SlotType.cs                         # 槽位枚举定义
│   └── DropResult.cs                       # 掉落判定结果（是否掉落、概率来源）
├── Extensions/
│   └── PlayerExtensions.cs                 # 扩展 Rocket 玩家对象的工具方法
├── Resources/
│   ├── ModifiedItemDrop.config.json        # Rocket 默认配置文件
│   ├── translations/
│   │   ├── en.json
│   │   └── zh-CN.json
│   └── ModifiedItemDrop.example.json       # 注释版样例配置（可选）
├── Docs/
│   ├── Project.md
│   └── README.md
├── Tests/                                  # 可选：计划编写单元测试时再创建
└── tools/                                  # 可选：脚本、打包或开发辅助工具
└── LICENSE
```

------

## 八、实现注意事项

- **热重载线程安全**：`ConfigurationLoader` 在更新配置时建议使用读写锁或原子替换，避免掉落流程读取半成品对象。
- **概率随机源**：默认可直接使用 `System.Random`，若需复现问题，再抽象为 `IRandomProvider`；无需提前拆分额外服务。
- **日志粒度控制**：默认只输出配置加载结果与掉落统计摘要，逐件物品的调试日志可通过配置开关启用，避免刷屏。
- **文档统一**：`Project.md` 作为设计说明，`README.md` 提供快速上手，避免两份文档内容完全重复。

------

## 九、逻辑流程

```
玩家死亡前触发事件
    ↓
读取玩家所有装备槽及背包内容
    ↓
对每件物品：
    ① 检查是否有单独概率配置（CustomItemChances）
    ② 否则取所属区域概率（RegionChances）
    ③ 否则取全局概率（GlobalDefaultChance）
    ↓
生成随机数进行判定
    ↓
若命中 → DropItem(物品, 玩家位置)
若未命中 → 跳过
```

------

## 十、未来扩展

- “禁止掉落列表” 与白名单物品
- 按玩家 Steam ID 或权限组定制概率（高优先级）
- 掉落日志持久化与统计面板（中优先级）
- 垂直扩展：按天气、地图区域调整倍率（低优先级，视需求量力而行）

------

## 十一、示例说明

------

## 十、示例说明

假设配置如上，玩家死亡时：

- 主武器掉落概率为七成
- 副武器掉落概率为三成
- 上衣物品掉落概率为四成
- 若某物品 ID 为十五，则优先用九成概率判定
- 所有未定义区域物品使用默认五成概率
