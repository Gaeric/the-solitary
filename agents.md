# agents.md

> 本文档用于指导 AI 编码代理（Claude Code / Codex 等）在 **TheSolitary**（Slay the Spire 2 Mod）项目上工作。
> 代理在读写本仓库文件时应优先遵守这里的约定。

## Project（项目概述）

**TheSolitary** 是一个基于 [STS2-RitsuLib](https://github.com/BAKAOLC/STS2-RitsuLib) 的 Slay the Spire 2（STS2）Mod，采用 **Godot 4.5.1 + C#（.NET 10 / C# 13）** 开发。

- 入口：`TheSolitaryCode/Entry.cs`，通过 `[ModInitializer]` 注册
- 内容：自定义角色 `TheSolitaryCharacter`（含卡牌池、遗物池、药水池）、4 张初始打击、4 张初始防御、初始遗物
- 内容通过 RitsuLib **自动注册**（`[RegisterCard]` / `[RegisterRelic]` / `[RegisterCharacter]`），无需在入口手动逐个注册

> **关于 Agent/LLM 架构**：经全量搜索，本仓库 **不包含任何 Agent/LLM 架构代码**——没有 LLM 调用、Prompt 模板或多 Agent 协作系统。游戏本体（`../sts2_20260821`）中只有 AutoSlay 自动战斗等传统游戏 AI，非 LLM Agent。本文件定位为「指导 AI 编码代理在本项目上工作的 AGENTS.md」。

## STS2 源码目录

- 本仓库：`d:\sts2_mods\the-solitary`
- STS2 完整源码 + 本地化：**`../sts2_20260821`**（绝对路径 `d:\sts2_mods\sts2_20260821`）——完整游戏工程，含 `src/Core/` 下的 C# 源码（`Models/Cards/`、`Commands/`、`Models/Powers/` 等）与 `localization/eng|zhs/` 下的**原版卡牌/关键词文本**。查游戏 API 与原版描述措辞都以这里为准。
- 旧反编译副本：`.tools/sts2_decomp/`（已在 `.gitignore`，仅供本地查阅；仅 C# 反编译、无本地化，新内容优先看 `../sts2_20260821`）

## 美术资源目录（WatcherBeautified）

- **`../WatcherBeautified`**（绝对路径 `d:\sts2_mods\WatcherBeautified`）是**观者（Watcher）角色美化包**的完整 Godot 工程——由 GDRE Tools 2.5.0 从 Steam 创意工坊的 `WatcherBeautified.pck`（应用 2868840 / 物品 3747800917）导出，引擎 **Godot 4.5.1**，共 1425 个文件（导出记录见 `gdre_export.log`）。
- **新增美术资源时，优先从这里取材**：把需要的 png / tres / tscn / ogg / skel 复制到本仓库 `TheSolitary/` 对应目录后再以 `res://TheSolitary/...` 引用。**不要**直接引用仓库外路径（不会进 pck）。

### 目录速查

| 资源 | 位置（相对 `../WatcherBeautified/`） | 说明 |
|---|---|---|
| 角色 Spine 骨骼动画 | `animations/characters/watcher/`（`skeleton.skel` / `skeleton.atlas` / `the_watcher.png` / `watcher_skel_data.tres`） | 战斗模型动画；选人界面版在 `animations/character_select/watcher/` |
| 角色场景 | `scenes/creature_visuals/watcher.tscn`、`scenes/merchant/characters/watcher_merchant.tscn`、`scenes/rest_site/characters/watcher_rest_site.tscn`、`scenes/screens/char_select/char_select_bg_watcher.tscn`、`scenes/ui/character_icons/watcher_icon.tscn` | 与 TheSolitary 的 `scenes/characters/` 一一对应 |
| 能量计数器 | `scenes/combat/energy_counters/watcher_energy_counter.tscn`、`scenes/vfx/energy/watcher/`、`images/ui/combat/energy_counters/watcher/`（能量球分层图） | TheSolitary 已有自己的能量球（5 层） |
| 卡框 / 材质 | `materials/cards/frames/card_frame_purple_mat.tres`、`materials/transitions/watcher_transition_mat.tres` | 紫色系，契合 TheSolitary 主题 |
| Power 图标 | `images/powers/*.png`（102 个）、`images/atlases/power_atlas.sprites/*.tres`（49 个） | png 可直接复制使用 |
| 遗物图标 | `images/relics/*.png`（+ `large/` + `outline/` 两套变体） | 三规格 |
| 药水图标 | `images/potions/*.png` | ambrosia / bottled_miracle / stance_potion |
| UI | `images/ui/charSelect/`（角色立绘）、`images/ui/hands/`（多人手型）、`images/ui/top_panel/` | |
| VFX | `images/vfx/`（divine_balance / stance / sts1 / sts1_eye）、`scenes/vfx/`（card_trail / energy vfx） | |
| 音频 | `audio/watcher/*.ogg`（姿态音效）、`audio/combat/*.ogg` | |
| 观者卡图（压缩纹理） | `Watcher/_imported/*.ctex`（434 个） | **观者全部卡图仅以 .ctex 压缩纹理存在**，需先转回 png 才能用作 `AssetProfile.PortraitPath`，不可直接引用 |

### 注意事项

- **角色模型是观者本体**（女性、长棍、紫色调）。TheSolitary 若直接套用会在视觉上「借用观者」，是否沿用由角色设计决定；但通用素材（Power/遗物/药水图标、卡框材质、UI、VFX、音频）可跨角色复用。
- **角色 Spine 动画已按官方教程接入**（`04-15-2 角色动画` + `05 卡图&Spine`）：
  - **Spine Godot Extension** 放在项目根目录 `bin/`（`spine_godot_extension.gdextension` + `windows/` dll），用**标准 Godot 4.5.1** 导出（`local.props` 的 `GodotExe` 已指回标准版；MegaDot 无法加载该扩展）。游戏自带的 `../sts2_20260821/addons/spine` 不要使用（会让编辑器崩溃）。
  - 观者骨骼文件在**顶层 `res://animations/characters/watcher/`**（`skeleton.skel` / `skeleton.atlas` / `the_watcher.png` / `watcher_skel_data.tres`）。注意：spine-godot 扩展的 `fix_path` 对 `res://TheSolitary/...` 会生成错误的三重斜杠，因此必须放在这个顶层路径（与源美化包一致）。
  - 战斗场景 `TheSolitary_character.tscn` 用 `SpineSprite` 节点；`TheSolitaryCharacter.SetupCustomCreatureAnimator` 用 `CreatureAnimator` 把标准状态映射到观者动画名 **`Idle`/`Attack`/`Cast`/`Hit`/`Dead`/`relaxed`**（注意大小写，与游戏标准 `idle_loop/attack/cast/hurt/die` 不同）。
  - 若移除 `bin/` 或改用无 Spine 引擎，导出会因无法解析 `SpineSkeletonDataResource` 而失败；静态方案可用 `VisualCueSet`（教程 04-15-2 的第一种方式）替代。
- 复制素材后，`*.png.import` / `*.uid` / `.godot` 由 Godot 重新生成（已在 `.gitignore`），**不要**把源工程的 `.import` 文件一起拷过来。
- 该目录在本仓库之外（`.gitignore` 管不到），按需引用；素材版权归属原美化包作者，对外发布前需确认授权。

## Setup（首次构建前）

1. 复制 `local.props.template` 为 `local.props` 并填写本机路径：

```powershell
Copy-Item .\local.props.template .\local.props
```

| 字段 | 说明 |
|---|---|
| `Sts2Dir` | STS2 游戏安装目录 |
| `Sts2DataDir` | 游戏 dll 目录，通常是 `$(Sts2Dir)/data_sts2_windows_x86_64` |
| `GodotExe` | 用于导出 pck 的 Godot（MegaDot 4.5.1 mono）可执行文件 |
| `RitsuLibDeployDir` | RitsuLib 本机部署目录（默认 `$(Sts2Dir)/mods/STS2-RitsuLib`） |

> ⚠️ `local.props` 已在 `.gitignore` 中，**不要提交**。缺失时构建会回退到 csproj 内的默认 `Sts2Dir`（Steam 默认路径）。

## Build（构建）

| 命令 | 行为 |
|---|---|
| `dotnet build .\TheSolitary.csproj` | 完整构建：编译 + `CopyMod`（拷贝 dll/manifest 到游戏 mods 目录）+ `ExportPCK`（导出 pck） |
| `... /p:RunPckExport=false` | 跳过 pck 导出（不需要 `GodotExe`） |
| `... /p:CopyModOnBuild=false` | 跳过拷贝到游戏 mods 目录（产物保留在 `bin/`） |
| `... /p:RunPckExport=false /p:CopyModOnBuild=false` | 仅验证 C# 编译 |

## Test / Run（测试与运行）

- 本项目**没有自动化单元测试**；验证方式为 `dotnet build` 编译通过 + 在 STS2 游戏内加载 Mod 实测。
- 完整构建后 Mod 部署到 `$(Sts2Dir)/mods/TheSolitary/`（dll + manifest + pck）。
- RitsuLib 本体由构建逻辑部署到 `$(Sts2Dir)/mods/STS2-RitsuLib/`。

## Format / Lint

- 无独立 format/lint 工具链；`.editorconfig` 仅要求 `charset = utf-8`（源码含中文注释，务必保持 UTF-8 编码）。

## Code style（代码风格约定）

- 文件头使用 **file-scoped namespace**：`namespace TheSolitary.Cards;`
- 内容类统一 `public sealed class`（卡片 / 遗物 / 角色）。
- 卡牌数值用 **const 字段**（`BaseEnergyCost`、`CardKind`、`CardRarityValue`、`CardTarget` 等）。
- 卡牌/遗物基础数值通过 `CanonicalVars` + `DynamicVar`（`DamageVar`、`BlockVar`、`CardsVar`）声明，自动绑定本地化占位符（`{Damage:diff()}`、`{Block:diff()}`）。
- 资源路径统一使用 `$"{Entry.ResPath}/images/..."`，文件名用 `{GetType().Name}`（与 png 同名）。
- 效果逻辑写 `protected override async Task OnPlay(...)`，用 `await ...Cmd...Execute(choiceContext)` 顺序执行；升级逻辑写 `protected override void OnUpgrade()`（`UpgradeValueBy(...)`）。
- 代码注释使用中文；新增内容需同时补充本地化文本。

## Project structure（目录结构）

```text
the-solitary/
├── TheSolitaryCode/          # C# 源码（Mod 逻辑）
│   ├── Entry.cs              # Mod 入口 [ModInitializer]
│   ├── Characters/           # TheSolitaryCharacter + 卡池/遗物池/药水池
│   ├── Cards/                # 卡牌类（Strike/Defend/轮回/负面效果符/无尽符/撒符等）
│   ├── Powers/               # Power 类（EndlessCharmPower 等）
│   └── Relics/               # 遗物类（SwiftCircuit 等）
├── TheSolitary/              # Godot 资源（images / localization / scenes），非 C#
│   ├── localization/
│   │   ├── eng/              # 英文：cards.json / characters.json / relics.json / powers.json / ancients.json
│   │   └── zhs/              # 简体中文（同上）
│   ├── images/               # 卡图 / 角色图 / 遗物图
│   └── scenes/characters/    # 角色 tscn 场景
├── TheSolitary.csproj        # MSBuild 工程（Godot.NET.Sdk/4.5.1, net10.0, C# 13）
├── TheSolitary.json          # Mod manifest（id / dependencies / min_game_version）
├── project.godot             # Godot 工程配置
├── local.props(.template)    # 本机路径配置（gitignored）
├── bin/                      # Spine Godot Extension（spine_godot_extension.gdextension + windows/ dll），导出必需；已被 .gitignore，克隆后需重新放入
├── animations/characters/watcher/  # 观者 Spine 骨骼（顶层路径，spine-godot 的 fix_path 需要）
├── character.org             # 角色设计文档（附魔/运转机制）
└── README.md                 # 使用与版本兼容说明
```

## Workflow（新增内容的标准流程）

新增一张卡牌 / 遗物时：

1. 在 `TheSolitaryCode/Cards/`（或 `Relics/`）新建类：
   - 卡牌：加 `[RegisterCard(typeof(TheSolitaryCardPool))]`；若是初始卡再加 `[RegisterCharacterStarterCard(typeof(TheSolitaryCharacter), 数量)]`。
   - 遗物：加 `[RegisterRelic(typeof(TheSolitaryRelicPool))]`；初始遗物再加 `[RegisterCharacterStarterRelic(typeof(TheSolitaryCharacter))]`。
2. 在 `TheSolitary/localization/eng/` 和 `zhs/` 对应 json 中补充键：
   - 键格式：卡牌 `THE_SOLITARY_CARD_<CLASS_NAME>.title` / `.description` / `.smartDescription`；遗物 `THE_SOLITARY_RELIC_<CLASS_NAME>.title`。
3. 数值文本用 `{Damage:diff()}`、`{Block:diff()}` 等占位符与 `CanonicalVars` 保持一致。
4. `dotnet build .\TheSolitary.csproj /p:RunPckExport=false` 验证编译通过。
5. 图片等素材直接使用WatcherBeautified中的资源

## 卡牌实现方法速查（2026-08 实战总结）

> 以下模式来自「凝滞 Stagnation / 环回形态 LoopForm / 附魔造物 EnchantedCreation / 撒符 ScatterCharms」等卡牌的实战验证。
> 新增卡牌时先确定要用的模式再套骨架；API 细节均已在 `../sts2_20260821/src/Core/` 源码与部署版 RitsuLib dll 核实。

### 0. 卡牌类通用骨架（ModCardTemplate）

```csharp
// TheSolitaryCode/Cards/XxxCard.cs
using MegaCrit.Sts2.Core.Commands;                       // PowerCmd / DamageCmd / CreatureCmd / PlayerCmd / CardCmd
using MegaCrit.Sts2.Core.Entities.Cards;                 // CardType / CardRarity / TargetType / CardKeyword / CardPlay / CardAssetProfile
using MegaCrit.Sts2.Core.GameActions.Multiplayer;        // PlayerChoiceContext
using MegaCrit.Sts2.Core.HoverTips;                      // IHoverTip / HoverTipFactory
using MegaCrit.Sts2.Core.Localization.DynamicVars;       // DynamicVar / PowerVar<T> / DamageVar / EnergyVar / CardsVar
using MegaCrit.Sts2.Core.Models.Powers;                  // SlowPower / WeakPower 等 Power 类型
using TheSolitary.Characters;                            // TheSolitaryCardPool
using STS2RitsuLib.Interop.AutoRegistration;             // RegisterCard
using STS2RitsuLib.Scaffolding.Content;                  // ModCardTemplate / CardAssetProfile

[RegisterCard(typeof(TheSolitaryCardPool))]   // 角色卡池；初始卡另加 [RegisterCharacterStarterCard(typeof(TheSolitaryCharacter), N)]
public sealed class XxxCard : ModCardTemplate
{
    private const int BaseEnergyCost = 1;
    private const CardType CardKind = CardType.Skill;           // Attack=攻击 / Skill=技能 / Power=能力
    private const CardRarity CardRarityValue = CardRarity.Uncommon; // Common=白 / Uncommon=蓝 / Rare=金 / Token=衍生(不进图鉴)
    private const TargetType CardTarget = TargetType.AnyEnemy;  // AnyEnemy 选一敌 / AllEnemies 全体 / Self 自身
    private const bool ShowInCardLibrary = true;                // Token 衍生牌=false

    public XxxCard() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary) { }

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");   // 卡图与类名同名

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust]; // 消耗；Innate=固有
    // ⚠️ RitsuLib 的 ExtraHoverTips 是 sealed！悬停提示必须覆写 AdditionalHoverTips
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<SlowPower>()];

    // 数值自动绑定本地化占位符（{Damage:diff()} / {SlowPower:diff()} / {Energy:energyIcons()} / {Cards:diff()}）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
        new PowerVar<SlowPower>(1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) { /* 效果 */ }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);   // 数值升级
        base.EnergyCost.UpgradeBy(-1);           // 费用 -1（参考计策/环回形态）
        AddKeyword(CardKeyword.Innate);          // 升级获得固有（参考无尽符）
    }
}
```

### 1. 单目标：造成伤害 + 施加 debuff（术式-凋零 ArtOfDecay / 术式-枯萎 ArtOfWilt / 瘟疫 Pestilence）

```csharp
await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
    .FromCard(this, cardPlay).Targeting(cardPlay.Target).Execute(choiceContext);
await PowerCmd.Apply<SlowPower>(choiceContext, cardPlay.Target, DynamicVars["SlowPower"].BaseValue, Owner.Creature, this);
```
- `DynamicVarSet` **没有 Slow 访问器** → 用索引器 `DynamicVars["SlowPower"]`（PowerVar 键名 = `typeof(T).Name`）；Weak/Vulnerable/Poison 有访问器（`DynamicVars.Weak.BaseValue`）。

### 2. 全体敌方施加（凝滞 Stagnation）

```csharp
// CardTarget = TargetType.AllEnemies（无手动选敌）
foreach (Creature e in CombatState!.HittableEnemies)
    await PowerCmd.Apply<SlowPower>(choiceContext, e, amount, Owner.Creature, this);
```
- 原版参考：Scare（全体虚弱+消耗）/ NegativePulse。`HittableEnemies` 只含存活可命中敌人。

### 3. 能力牌 + 事件钩子 Power（环回形态 LoopForm / 附魔造物 EnchantedCreation / 无尽符 EndlessCharm）

卡牌端：`CardKind=Power`、`CardTarget=Self`，OnPlay 里 `PowerCmd.Apply<XxxPower>(..., 1m, ...)`（EndlessCharm 用 `PowerUp` 动画）。

Power 端（`TheSolitaryCode/Powers/`，`[RegisterPower]` + `ModPowerTemplate`）：

```csharp
[RegisterPower]
public sealed class XxxPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 常用事件钩子（AbstractModel 虚方法，覆写即可）：
    // - AfterShuffle(PlayerChoiceContext, Player)              —— 抽牌堆被洗牌时（环回形态）
    // - AfterCardGeneratedForCombat(CardModel, Player?)        —— 玩家生成一张牌时（附魔造物）
    // - BeforeHandDraw(Player, PlayerChoiceContext, ICombatState) —— 回合开始时（无尽符）
    public override async Task AfterShuffle(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner.Player) return;      // 先校验触发者归属
        Flash();
        await PlayerCmd.GainEnergy(base.Amount, base.Owner.Player);   // 每层 Amount 生效
    }
}
```
- 钩子内**先校验归属再 `Flash()`**（原版 StratagemPower / PillarOfCreationPower 同款）。
- 生成牌钩子额外注意：`creator.Creature == Owner`（排除敌人施加的状态牌）+ `card.Enchantment == null` + 防递归标志（参考 SoulboundPower 的 `IsAddingSoul` 模式）。
- `ModPowerTemplate` 悬停提示：能量类设 `protected override bool IncludeEnergyHoverTip => true`（自动显示 Amount）；其余用 `AdditionalHoverTips`。
- 可空警告按需加 `!`：`Owner.Player!` / `CombatState!` / `Owner.PlayerCombatState!`。

#### 3.1 每个副本单独计数（`PowerInstanceType.Instanced`）

默认 `PowerInstanceType.None` 时，重复施加同一 Power 只会**叠加 Amount**（图标只有一个、进度计数共享）。
若要求「每一个打出的副本单独计数、单独显示」（例：元能吸附 EnergyAbsorptionPower），覆写：

```csharp
public override PowerStackType StackType => PowerStackType.Counter;
// 每次施加都新建独立实例而非叠加层数（原版神气制胜 PanachePower / 定时炸弹 TheBombPower 同款）。
public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
```

- `PowerCmd.Apply<T>` 走 `FindExistingInstanceForStacking`：`Instanced => null` → 每次都 `ToMutable()`，
  因此每个实例拥有**自己的 `DynamicVars`**（`{EnchantedCardsLeft}` 之类的进度占位符互不影响）与自己的私有数据。
- 私有数据用 `protected override object? InitInternalData() => new Data();` + `GetInternalData<Data>()`；
  克隆/施加时重新初始化，**不会**出现在本地化里（要显示的数值放 `DynamicVars`）。
- 施加本能力的卡牌刚打出时 `AfterCardPlayed` 会立刻触发（`CardModel.Play` 里 OnPlay 之后才 `Hook.AfterCardPlayed`），
  新建的实例会把这张牌本身算进进度 → 参考 PanachePower 的 `Data.alreadyApplied` 首次跳过。
- UI（`NPowerContainer`）按 `Creature.Powers` 逐个实例建图标，不做同 Id 去重，所以多个副本会各显示一个带各自数字的图标。
- 结算总量不变：N 个副本 × 每副本 5 张附魔牌 = 每 5 张附魔牌共获得 N 点能量。

### 4. 生成牌随机附魔（附魔造物 EnchantedCreation）

- 随机附魔池抽成共享静态类 `TheSolitaryCode/Cards/RandomEnchantPool.cs`（`EnchantRandomly(Rng, CardModel)`），供献祭 Sacrifice / 附魔造物复用，避免两处池子漂移。
- 池子：Sharp 2 / Momentum 1 / Instinct 1 / Spiral 1 / Adroit 2 / Nimble 2；先 `ModelDb.Enchantment<T>().ToMutable().CanEnchant(card)` 过滤，再用 `Rng.NextItem` 随机，最后 `CardCmd.Enchant<T>(card, amount)` 施加。

### 5. 原版措辞规范（查 `../sts2_20260821/localization/eng|zhs/cards.json`）

- 生成牌类：EN *"Whenever you create a card, ..."*；ZH *"每当你生成一张牌时，..."*（参考创世之柱 PILLAR_OF_CREATION / 烟囱 SMOKESTACK / 锋利边缘 SHARP_EDGE）。
- 全体类：EN *"... to ALL enemies."*（中文"对所有敌人..."）。
- 附魔动词用本 Mod 既有约定：*"Enchant ... with [gold]X[/gold]"*（参考附魔抽牌 EnchantedDraw）。
- 消耗/固有等 `CardKeyword` 自动显示在卡面，**不必写进描述**。
- 占位符与 `CanonicalVars` 一一对应：`{Damage:diff()}` `{Block:diff()}` `{Cards:diff()}` `{Energy:energyIcons()}` `{SlowPower:diff()}`（= PowerVar 键名）等。

### 6. 卡图（WatcherBeautified）

- **直接可用的 png**：`../WatcherBeautified/images/packed/card_portraits/watcher/*.png`（观者全卡图，如 cataclysm / deva_form / omniscience）。`Watcher/_imported/*.ctex` 是压缩纹理，别直接用。其中 `draw_talisman` / `preach` / `serenity` 三张是**先古卡尺寸 606×852（竖向）**，见 6.2。
- 游戏卡框尺寸 **500×380（横向）**，**先古卡为 606×852（竖向，见 6.2）**；源图若已是目标尺寸（如 deva_form、omniscience、draw_talisman）直接复制；竖版（1058×1487 等）需**居中裁剪到 500:380 再缩放**避免变形：

```powershell
Add-Type -AssemblyName System.Drawing
$src=[System.Drawing.Image]::FromFile($srcPath); $sw=$src.Width; $sh=$src.Height
$tr=500/380.0; $sr=$sw/$sh
if($sr -gt $tr){$cw=[int]($sh*$tr);$ch=$sh}else{$cw=$sw;$ch=[int]($sw/$tr)}
$x=[int](($sw-$cw)/2); $y=[int](($sh-$ch)/2)
$bmp=New-Object System.Drawing.Bitmap 500,380
$g=[System.Drawing.Graphics]::FromImage($bmp)
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.DrawImage($src,(New-Object System.Drawing.Rectangle 0,0,500,380),(New-Object System.Drawing.Rectangle $x,$y,$cw,$ch),[System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose(); $bmp.Save('...\XxxCard.png',[System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose(); $src.Dispose()
```
- 复制/替换 png 后**删除旧的 `XxxCard.png.import`**，Godot 导出时会重新生成。
#### 6.1 卡图占用检测与区分度校验（2026-08 实战总结）

> 场景：给某张卡换卡图时，必须保证**不与任何现有卡图撞图**、且与目标区分对象（常一起出现/一起比较的卡）视觉差异足够大。
> **教训：绝不要只按文件大小判断占用**——重编码后的图大小会变。实战案例：`EnchantedCreation.png`（401533B）与源图 `omniscience.png`（317282B）大小不同，但像素差仅 8.3，实为同一张图。占用以**缩略特征比对**为准。
> 观者卡图同属紫/金色系：即使平均像素差 300+ 的两张图（如 `mental_fortress` 与 `hymn`），构图相近时玩家仍会觉得"一样"。判断"像不像"要**像素差 + 主色调色系**双看。

**① 两图像素差异**（0=同一张图，765=完全不同；<15 视为同图，15~200 易看混，>250 差异明显）：

```powershell
Add-Type -AssemblyName System.Drawing
$a=[System.Drawing.Bitmap]::FromFile('A.png'); $b=[System.Drawing.Bitmap]::FromFile('B.png')
$diff=0.0; $n=0
for($x=0;$x -lt 500;$x+=6){ for($y=0;$y -lt 380;$y+=6){
    $pa=$a.GetPixel($x,$y); $pb=$b.GetPixel($x,$y)
    $diff += [Math]::Abs($pa.R-$pb.R)+[Math]::Abs($pa.G-$pb.G)+[Math]::Abs($pa.B-$pb.B); $n++ } }
$a.Dispose(); $b.Dispose()
"Avg pixel diff (0=identical, 765=max): {0:N1}" -f ($diff/$n)
```

**② 主色调（平均 RGB）**——与目标卡同色系（如都是蓝紫）即使像素差大也易看混，优先选色系反差大的：

```powershell
Add-Type -AssemblyName System.Drawing
function AvgColor($p){
    $i=[System.Drawing.Bitmap]::FromFile($p); $r=0L;$g=0L;$b=0L;$n=0
    for($x=0;$x -lt $i.Width;$x+=5){ for($y=0;$y -lt $i.Height;$y+=5){
        $c=$i.GetPixel($x,$y); $r+=$c.R;$g+=$c.G;$b+=$c.B;$n++ } }
    $i.Dispose(); [PSCustomObject]@{R=[int]($r/$n);G=[int]($g/$n);B=[int]($b/$n)} }
AvgColor 'xxx.png'   # 参考：共轭 mental_fortress=(57,135,213)蓝紫；万物通元 omniscience=(134,93,69)金棕
```

**③ 全量占用检测 + 区分度排名**（把观者源图池 185 张与现有卡图 82 张全部比对，挑"与每张现有卡差异都大"的可用图）：

```powershell
Add-Type -AssemblyName System.Drawing
# 500x380 缩略为 20x15 特征向量（900 int），可靠且快
function Get-Feat($path){
    $src=[System.Drawing.Bitmap]::FromFile($path)
    $bmp=New-Object System.Drawing.Bitmap 20,15
    $g=[System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.DrawImage($src,0,0,20,15); $g.Dispose(); $src.Dispose()
    $feat=New-Object int[] (900); $idx=0
    for($y=0;$y -lt 15;$y++){ for($x=0;$x -lt 20;$x++){
        $c=$bmp.GetPixel($x,$y); $feat[$idx]=$c.R; $feat[$idx+1]=$c.G; $feat[$idx+2]=$c.B; $idx+=3 } }
    $bmp.Dispose(); return ,$feat }
function FeatDiff($f1,$f2){ $s=0.0; for($i=0;$i -lt 900;$i++){ $s += [Math]::Abs($f1[$i]-$f2[$i]) }; $s/900.0 }

$existing=@{}; $skip='Reverb'   # ← 改成要替换的目标卡类名
foreach($png in Get-ChildItem 'TheSolitary\images\cards\*.png'){ if($png.BaseName -ne $skip){ $existing[$png.BaseName]=Get-Feat $png.FullName } }

$out=New-Object System.Collections.Generic.List[object]
foreach($src in Get-ChildItem '..\WatcherBeautified\images\packed\card_portraits\watcher\*.png'){
    $feat=Get-Feat $src.FullName; $minName=''; $minD=[double]1e9
    foreach($k in $existing.Keys){
        $d=FeatDiff $feat $existing[$k]; if($d -lt $minD){ $minD=$d; $minName=$k } }
    $out.Add([PSCustomObject]@{Source=$src.BaseName;Nearest=$minName;MinDiff=[int]$minD}) }
$out | Sort-Object MinDiff -Descending | Select-Object -First 30 | Format-Table -AutoSize
```

- `MinDiff` = 该源图与「最接近的一张现有卡图」的缩略差异，**越大越独特**，>50 基本可安全使用。
- 选图后用脚本 ① ② 再与目标区分对象核对一次；确认尺寸正确——**普通卡 500×380（横向）**、**先古卡 606×852（竖向，见 6.2）**（或按上文裁剪脚本处理）。
- 换图流程：`Copy-Item` 覆盖 → **删除 `XxxCard.png.import`**（Godot 重新导入）→ 完整构建导出 pck（仅 `dotnet build /p:RunPckExport=false` 不更新游戏内卡图）。

#### 6.2 先古（Ancient）卡图尺寸 = 606×852（竖向整幅画）

> 教训（2026-09）：`CardRarity.Ancient` 的卡走**完全不同的卡面布局**，卡图尺寸与普通卡不同——普通卡 **500×380（横向，原版源图 1000×760）**，先古卡必须是 **606×852（竖向）**。曾把 `Revelation` / `Resurgence` 卡图做成 500×380，游戏里被硬拉伸变形（"图很怪"）。

- **判定与布局**（`Reload`，`../sts2_20260821/src/Core/Nodes/Cards/NCard.cs:738`）：`Model.Rarity == CardRarity.Ancient` 时隐藏 `Portrait` / `PortraitBorder` / `Frame` / `Banner`，改显示 `AncientPortrait` + `AncientBorder` + `AncientBorderGlassOverlay` + `AncientTextBg` + `AncientBanner`（后四者贴图由游戏按 `CardType` 自动挑，本 Mod 不必提供）。
- **为什么会变形**：`scenes/cards/card.tscn` 的 `AncientPortrait` 是 `expand_mode = 1`（IGNORE_SIZE）且**没有 `stretch_mode`（默认 `SCALE` = 直接拉满矩形）**，矩形约 598×842（`offset_left/top/right/bottom = -153/-215/445/627`，`scale = 0.5`）；普通 `Portrait` 才是 `stretch_mode = 5`（KEEP_ASPECT_CENTERED，等比）。**先古卡图宽高比不对就是硬拉伸，不会等比缩放。**
- **正确尺寸**：原版全部先古卡图都是 **606×852**（606/852 = 0.711 ≈ 矩形 598/842 = 0.710，肉眼无变形），例 `corruption` / `apotheosis` / `wish` / `wraith_form` / `the_sealed_throne`，占位图 `ancient_beta.png` 同尺寸。原版卡图尺寸分布（PNG 头 offset 16..23 为宽高，比 System.Drawing 快、适合全量扫描）：`1000×760`（普通卡源图，684 张；图集精灵另存为 668×508）/ **`606×852`（先古卡）** / `500×380`（少数旧素材）/ `668×936`（先古卡 beta）。
- **RitsuLib 没有「先古卡图」字段**：`CardAssetProfile` 只有 `AncientBorderPath` / `AncientTextBgPath` / `AncientBannerPath`（+ 各自 `*Material*`）与 `VisualStyle`（默认 `CardVisualStyle.Default` = 沿用游戏本体稀有度判定，**别改**），卡面肖像仍共用同一个 `PortraitPath` → **先古卡只能把 `PortraitPath` 直接指向 606×852 的图**。
- **现成素材（606×852、观者主题、本 Mod 未占用）**：`../WatcherBeautified/images/packed/card_portraits/watcher/` 下的 `draw_talisman.png`（卡片从法杖/符中爆出）、`preach.png`（巨手赐福跪拜者）、`serenity.png`（606×851，观者角色立绘）。直接 `Copy-Item` 覆盖即可，**无需裁剪**（观者池其余 263 张都是 500×380 横向，不能用于先古卡）。当前用法：`Revelation` ← `draw_talisman`，`Resurgence` ← `preach`。
- 替换后照例**删除旧 `XxxCard.png.import`** → 完整构建（含 pck 导出）才进游戏。

### 7. 反编译工具（ilspycmd）

```powershell
# 反编译指定类型（查 RitsuLib 模板类 / 任意 dll 的 API 表面）
dotnet '.\.tools\.store\ilspycmd\10.1.1.8388\ilspycmd\10.1.1.8388\tools\net10.0\any\ilspycmd.dll' -t '<全限定类型名>' '<dll路径>'
# 例：RitsuLib ModCardTemplate / ModPowerTemplate（部署版 dll）
dotnet ...\ilspycmd.dll -t 'STS2RitsuLib.Scaffolding.Content.ModCardTemplate' 'D:\Program Files\Steam\steamapps\common\Slay the Spire 2\mods\STS2-RitsuLib\STS2-RitsuLib.dll'
```

### 8. 命名空间 / API 常见坑

- **`CardModel` 在 `MegaCrit.Sts2.Core.Models`**（不在 `Entities.Cards`！），引用前确认 using。
- RitsuLib `ModCardTemplate` / `ModPowerTemplate` 的 `ExtraHoverTips` 均 **sealed** → 一律用 `AdditionalHoverTips`。
- `PowerVar<T>` 的 DynamicVar 键名 = `typeof(T).Name`（`PowerVar<SlowPower>` → `"SlowPower"`）。
- 事件钩子全部定义在 `AbstractModel`（Power/遗物/卡牌都能覆写）；触发点在 `../sts2_20260821/src/Core/Commands/CardPileCmd.cs` 等。
- 本地化键：卡牌 `THE_SOLITARY_CARD_<CLASS_NAME>.title/.description/.smartDescription`；Power `THE_SOLITARY_POWER_<CLASS_NAME>_POWER.*`。

### 9. Harmony 补丁技法（RitsuLib `IPatchMethod`，已实测验证）

> 验证环境：游戏自带 `data_sts2_windows_x86_64/0Harmony.dll` + 游戏运行时 .NET 9（见 `sts2.runtimeconfig.json`）。
> ⚠️ 这套 0Harmony **不支持 CoreCLR 10**（`PlatformNotSupportedException: CoreCLR version 10.x is not supported`），
> 用 `dotnet run` 做对照实验时测试工程必须 `<TargetFramework>net9.0</TargetFramework>`。

- **目标解析**：`new ModPatchTarget(type, methodName, parameterTypes, ignoreIfMissing)`；
  `parameterTypes` 建议显式给全（如 `[typeof(PlayerChoiceContext), typeof(CardPlay)]`），传 `null` 时 `GetMethod` 可能歧义。
  `public/protected/private` 方法都能命中（内部用 `instance|static|public|nonpublic`）。
- **改写原方法参数（prefix）**：形参声明为 `ref`（**名字必须与原方法形参一致**），写入在进入原方法前生效，**对 `async` 方法同样有效**（状态机拿到的是改写后的对象）：
  ```csharp
  public static bool Prefix(ref CardPlay? cardPlay) { cardPlay = CopyWithTarget(cardPlay!, target); return true; }
  ```
- **跳过原方法**：prefix `return false`。若原方法返回 `Task`，**必须**同时给 `ref Task __result` 赋一个已完成 Task，
  否则调用方 `await` 会拿到 null 抛 NRE：
  ```csharp
  public static bool Prefix(ref Task __result) { __result = Task.CompletedTask; return false; }  // 不执行原方法
  ```
- **只做后置效果**：用 `async void Postfix(...)` + `try/catch`（异常必须自己吞掉，否则崩游戏；见 `AfterEnchantPatch`）。
- 新增 `IPatchMethod` 类后**必须在 `Entry.Initialize()` 里 `CreatePatcher` + `RegisterPatch<T>` + `PatchAll()`**，并保持 `PatchId` 唯一。

### 10. 任务牌（Quest）与卡牌转换（勤学 DiligentStudy → 博识 Erudition；超渡 Requiem）

> 对照原版 探寻 Dowsing → 富足 Abundance；超渡 Requiem 对照原版诅咒 愧疚 Guilty。参考实现：`TheSolitaryCode/Cards/DiligentStudy.cs`、`TheSolitaryCode/Cards/Erudition.cs`、`TheSolitaryCode/Cards/Requiem.cs`。

- **任务牌骨架（本 Mod 超渡 / 勤学；2026-09 曾改成基础牌，随后按要求改回任务牌）**：`CardType.Quest` + `CardRarity.Quest` + 费用 `-1` + `CanonicalKeywords => [CardKeyword.Unplayable]` + `MaxUpgradeLevel => 0` + `CanBeGeneratedInCombat => false`，注册进**原版 `QuestCardPool`**（`[RegisterCard(typeof(QuestCardPool))]`，与探寻同池）→ 不进奖励 / 商店 / 随机变化 / 战斗生成。**改回任务牌的代价要清楚**：原版只按 `Rarity == CardRarity.Basic` 判定"基础牌"（`Models/Events/WoodCarvings.cs` 木雕的变换、`CardFactory.FilterForCombat`），所以任务牌吃不到这类效果；而且 `CardFactory.GetDefaultTransformationOptions` 对 `CardType.Quest` 强制改用无色卡池、`CardSelectCmd.FromDeckForTransformation` 直接排除 `CardType.Quest`。**若将来又要让它们参与"变更 / 变换"玩法**，需要重新评估改成 `CardType.Skill` + `CardRarity.Basic` + 角色卡池（那就同时会获得基础牌的两个副作用：被 `CallOfTheVoidPower` 随机生成 / `ThievingHopper` 偷牌 / `SlipperyBridge` 优先夺走时被跳过——对本 Mod 是有利副作用；也会被木雕变换成同卡池的普通 / 罕见 / 稀有牌）。
- **进度持久化**：用 `[SavedProperty]`（`using MegaCrit.Sts2.Core.Saves.Runs;`）存进度。`SavedProperties.From` 只收集 `[SavedProperty]` 属性，**不带该特性的自定义字段不会随存档保存**（原版 Dowsing 的 `RoomsEntered` 同款）；setter 里顺手刷新 `DynamicVars["X"].BaseValue` 让 `{X:diff()}` 实时反映进度。
- **房间计数**：覆写 `BeforeRoomEntered(AbstractRoom room)`（触发链：`RunManager` → `Hook.BeforeRoomEntered` → `runState.IterateHookListeners(null)`，**包含牌组里的牌**）。必须先判 `Pile != null && Pile.Type == PileType.Deck`，否则战斗中抽到的克隆也会计数；`MapRoom`（地图界面）不是房间，用 `room switch` 返回 -1 过滤掉。
- **同一个房型要进多次（本 Mod 勤学：5 种房型各 2 次 = 10 次）**：用一个 `[SavedProperty] int RoomVisits` 按"每种房型 2 位（0~2）"打包计数（`VisitCountAt` / `WithVisitCountAt` / `CountTotalVisits`，读时用 `Math.Min` 夹紧上限），进满的房型直接 `return` 不再计数，总和到 `TotalRequiredVisits` 才 `TransformTo<Erudition>`；卡面显示 `{Times:diff()} = 10 - 已历次数`，`{Types:diff()}` / `{Visits:diff()}` 是两个固定值（5 / 2）。悬停提示按"还差 2 次 / 1 次"选两条静态文案（`missingRoomTwice` / `missingRoomOnce`），不必给 `HoverTip` 做动态格式化。**改 `[SavedProperty]` 的编码语义时务必一起换属性名**：`SavedProperties.FillInternal` 是按属性名反射赋值（`GetProperty(name)?.SetValue`），名字对不上就静默忽略——换名不会让旧存档报错，但沿用旧名会把旧数据误读成新语义。
- **转换（永久）**：`PlayerCmd.CompleteQuest(card)` + `await CardCmd.TransformTo<T>(card)`。`TransformTo<T>` 用 `original.CardScope.CreateCard<T>` 建替换牌，要求原牌在牌堆中且 `IsTransformable`（带 `Eternal` 的牌不可转换）；**与来源牌的 `CardType` / `CardRarity` 无关**，所以基础牌也能当转换源（`PlayerCmd.CompleteQuest` 只往当前地图点的 `CompletedQuests` 历史里记一笔 id，不校验卡牌类型）。
- **"N 场战斗后…"类效果（计战斗 + 移出牌组 + 给奖励）**：与原版诅咒 愧疚 `Guilty` 保持一致——覆写 `AfterCombatEnd(CombatRoom room)`（战斗**结束**时触发：`CombatManager.EndCombatInternal` → `Hook.AfterCombatEnd`，`runState.IterateHookListeners(combatState)` **包含牌组里的牌**），先判 `Pile != null && Pile.Type == PileType.Deck`（战斗中抽到的克隆牌不算），计数存 `[SavedProperty]`，显示用 `DynamicVars["Combats"].BaseValue = 上限 - 已历场数`。**不要**用 `BeforeRoomEntered` 数 `CombatRoom`，那会变成"进入战斗"而不是"打完战斗"（事件里只展示敌人的战斗式布局不创建 CombatRoom，但语义仍以 `AfterCombatEnd` 为准）。达成后的结算顺序照原版任务牌 宝藏图 `SpoilsMap.OnQuestComplete`：`await PlayerCmd.GainGold(...)` → `PlayerCmd.CompleteQuest(this)` → `await CardPileCmd.RemoveFromDeck(this)`（`RemoveFromDeck` 要求牌确实在牌组里）。参考实现：`TheSolitaryCode/Cards/Requiem.cs`（超渡）。**只统计"普通战斗"**：`AfterCombatEnd` 里先 `if (room.RoomType != RoomType.Monster) return;`（`RoomType` 在 `MegaCrit.Sts2.Core.Rooms`，`Monster` = 普通战斗、`Elite` / `Boss` 不计入，判定同原版 鱼竿 `FishingRod`），否则精英 / Boss 战也会推进进度。
- **衍生牌（Token）**：`CardRarity.Token` + 注册进原版 `TokenCardPool` + `ShowInCardLibrary = false`（与术式 / 小刀 Shiv 同类）→ 不进奖励、商店、随机变化、战斗生成，也不进图鉴；`CanBeGeneratedInCombat => false` 再挡一层 `CardFactory.FilterForCombat`。**注意卡框与能量图标会跟着 Token 池变成无色**。
- **战斗内永久改牌组**：`cardPlay.Card.DeckVersion` 才是牌组原件（战斗克隆结束时被丢弃）。替换牌必须建在**运行作用域**：用 `Owner.RunState.CreateCard(canonical, owner)` 而不是 `CardModel.CardScope`（战斗中它优先返回**战斗**作用域，建出来的牌战斗结束会被清理）。变换后取 `CardPileAddResult.cardAdded`（可能被"加入牌组"钩子替换过）再继续后续操作。
- **绕过 CanEnchant 附魔**：`EnchantHelpers.ApplyEnchantmentBypassingCanEnchant(card, ModelDb.Enchantment<T>().ToMutable())`（例：注能 `Imbued` 只允许附魔技能牌，却要附给能力牌）。
- **任务牌也可以当初始卡**：`[RegisterCharacterStarterCard(typeof(TheSolitaryCharacter), N, Order = k)]`（RitsuLib 的补丁挂在角色 `StartingDeck` getter 上，**与卡牌所属卡池无关**，所以注册在 `QuestCardPool` 的任务牌同样能进初始牌组）。`Order` 是注册管线内的排序（越小越先，默认 0；本 Mod 现有：打击/防御 0 → 唤醒/匣中术 1 → 勤学 2 → 超渡 3），决定初始牌组里卡的追加顺序。参考实现：`DiligentStudy.cs` / `Requiem.cs`。
- **悬停提示列状态**：`AdditionalHoverTips` 可用迭代器按当前状态动态产出；房间名等文本直接复用原版 `static_hover_tips` 表的 `ROOM_*.title`（`new LocString("static_hover_tips", "ROOM_ENEMY.title")`，中英随语言自动切换），自定义说明放本 Mod 的 `cards` 表。

## 关键注意事项（Gotchas）

- **`Entry.cs` 的 `ModId` 必须与 `TheSolitary.json` 的 `id` 一致**（当前都是 `TheSolitary`）。
- `res://TheSolitary/...` 中的 `TheSolitary` 是 **PCK 资源目录名**，不是 C# namespace。
- 构建时会运行 `SyncManifestDependencies`，自动把 `TheSolitary.json` 的 `dependencies[STS2-RitsuLib].version` 同步为实际 NuGet 版本；但 **`min_game_version` 仍需人工核对**（当前 `0.106.0`）。
- 三个 RitsuLib 包（主线 + Compat）**一次只能启用一个**；主线 `STS2.RitsuLib` 仅支持 STS2 0.105.0+。
- `.csproj` 通过 `Krafs.Publicizer` 公开 sts2 内部成员；游戏 dll 从 `$(Sts2DataDir)` 引用（`0Harmony.dll` / `sts2.dll` / `Steamworks.NET.dll`）。
- `local.props`、`.tools/`、`*.uid`、`*.import` 都在 `.gitignore` 中，不要提交。
- 美术资源取材自 `../WatcherBeautified`（观者美化包，GDRE 导出的 Godot 工程），详见「美术资源目录」一节。
- 查阅游戏 API 与原版描述措辞优先参考 `../sts2_20260821`（源码 + 本地化）；旧 `.tools/sts2_decomp/` 仅为残留反编译副本。
- **附魔对卡牌的改写不会随「移除附魔」自动还原**：游戏清除附魔（`CardCmd.ClearEnchantment` → `CardModel.ClearEnchantmentInternal` → `EnchantmentModel.ClearInternal`）只摘除附魔引用，`EnchantmentModel` 里**没有任何 `UnmodifyCard` / 移除附魔回调**，所以 `OnEnchant` 的改写会留在原卡上：① 余烬 `TezcatarasEmber` 用 `EnergyCost.UpgradeBy` 把基础费用永久改写成 0 并加 `Eternal`；② 灵魂之力 `SoulsPower` 移除 `Exhaust`；③ 黏糊 `Goopy` / 稳定 `Steady` / 御准 `RoyallyApproved` 添加 `Exhaust` / `Retain` / `Innate`。原版没有移除附魔的机制所以碰不到，但本 Mod 的「轮回」「拟合」「换位」会清除并交换附魔。本 Mod 的处理方式：用两个 Harmony 补丁在**施加时**把「附魔前状态」记进附魔 `Props`，再由 `EnchantHelpers` 在清除后还原：
  - `Patches/TezcatarasEmberCostRecordPatch.cs` →「附魔前费用」→ `EnchantHelpers.TryGetEmberOriginalCost` / `RestoreCardAfterEmberRemoved`（只恢复费用；永恒改由下面的关键词快照还原，**不要再无条件 `RemoveKeyword(Eternal)`**——`RemoveKeyword` 直接改 `LocalKeywords`，会把卡牌自带的永恒一起删掉）。
  - `Patches/EnchantKeywordRecordPatch.cs`（挂在抽象基类 `EnchantmentModel.ModifyCard`，即 `OnEnchant` 的唯一调用点，施加 / 读档 / 交换都会走到）→「附魔前的本地关键词集合」→ `EnchantHelpers.TryGetKeywordsBeforeEnchant` / `RestoreCardKeywordsAfterEnchantmentRemoved`（与当前关键词双向求差：附魔移除掉的加回来、附魔添加的去掉）。
  - `EnchantHelpers.ReplaceEnchantment`（单张牌的替换原语：交换 / 轮回 / 拟合 / 融会贯通都走它）的固定顺序：`RemovedEnchantmentSideEffects.Capture(旧附魔)` → `CardCmd.ClearEnchantment` → `Restore(原卡)` → `ApplyEnchantment(新附魔)`。**还原必须排在施加新附魔之前**，否则新附魔添加的关键词会被当成"残留差异"删掉。`SwapEnchantmentsBetweenTwoCards` 现在只负责「先把两张牌的附魔各自快照为全新实例，再各调一次 `ReplaceEnchantment`」（顺序执行与原先"两牌同时清除再分别施加"等价：两张牌的附魔实例互不相同，替换第一张不会影响第二张的快照记录）。
  - `EnchantmentModel.Props` 只是挂在模型实例上的内存袋：`SavedProperties.From` 只收集 `[SavedProperty]` 属性，所以它随 `RebuildEnchantment`（`ToSerializable`/`FromSerializable`）**不保留**——好在补丁会在每次 `ModifyCard` 时重写记录，因此交换链 A→B→C 不会读到上一张牌的旧记录；但反过来说，**记录只在"当前这次施加之后、这次清除之前"有效**，别指望它跨重建/跨存档读取。
  - **以后任何「移除/交换附魔」的逻辑都必须走 `EnchantHelpers` 的交换路径（`SwapEnchantmentsBetweenTwoCards` / `SwapEnchantmentWithHandCard` / `ShuffleEnchantmentsInCards`，或直接复用 `ReplaceEnchantment`），或复用 `RemovedEnchantmentSideEffects` 的 `Capture`/`Restore`**，不要直接 `CardCmd.ClearEnchantment`，否则 0 费 / 永恒 / 消耗 / 保留等改写会残留在原卡上。
  - `EnchantHelpers.ShuffleEnchantmentsInCards(cards, rng)`（融会贯通 Mastery）：≥2 张时按**随机错排**（`CreateRandomDerangement`，Fisher-Yates + 修复固定点）用两两交换把排列实现出来 → 每张牌必定换到"别人原来的附魔"；**恰 1 张时改为把这唯一一张牌的附魔重新充能**（`ReplaceEnchantment(card, RebuildEnchantment(card.Enchantment))`，一次性 Status 复位）；0 张时无事发生。
- **RitsuLib Harmony 补丁必须显式注册**：新增 `IPatchMethod` 类后，必须在 `Entry.Initialize()` 里 `RitsuLibFramework.CreatePatcher(...)` + `patcher.RegisterPatch<T>()` + `patcher.PatchAll()`（参考 `AfterEnchantPatch` / `TezcatarasEmberCostRecordPatch` / `EnchantKeywordRecordPatch` / `InkyTargetPatch` 的注册）。不会自动发现；漏注册表现为「编译通过但补丁不生效」（启动日志里看不到对应的 `Patch application complete` 行）。
- **墨影（Inky）的虚弱在原版有两个毛病**：① `Inky.OnPlay` 只对 `TargetType.AllEnemies` 用 `HittableEnemies`，其余一律取 `cardPlay.Target`；而 `RandomEnemy`（随机多段攻击牌的目标类型，如 光子映射 / 飞剑回旋镖 / 星尘）与 `Self`/`None`/`AnyPlayer` 等牌**不选目标**，`cardPlay.Target == null` → `[null]` 进 `PowerCmd.Apply` → `target.CanReceivePowers` 抛 `NullReferenceException`（原版墨影只由 BladeOfInk 发给 AnyEnemy/AllEnemies 的小刀，所以这条路径在原版没被踩到）。② 原版每次打牌最多只施加 1 层（每个目标一次），表达不了"每命中一次加一层虚弱"。本 Mod 用两个补丁解决（都在 `Entry.Initialize()` 里注册到 `inky-target` patcher）：
  - `Patches/InkyHitRecordPatch.cs`：前缀挂 `Hook.AfterDamageGiven`（**每次命中/每个目标都会触发一次**，含被完全格挡的命中），把"这张墨影牌本次打牌命中过的敌人"按 `(卡牌实例, CurrentPlayIndex)` 登记进 `InkyHitRegistry`（`ConditionalWeakTable` 弱引用，战斗结束自动回收）。**列表不去重**（命中几次记几次）。
  - `Patches/InkyTargetPatch.cs`：前缀挂 `Inky.OnPlay`。**有命中登记（任意目标类型）→ 命中几次就对命中的敌人施加几次 1 层虚弱**（同一敌人多段命中叠多层；指定敌人的多段牌同样按段数叠），施加封装成原方法要返回的 `Task`，仍被打牌流程 `await`（时机与原版一致：本次伤害结算完之后，不会反过来增强本次多段伤害）；本次没造成伤害时退回原版（有目标 → 给该目标 1 层；`AllEnemies` → 所有可命中敌人各 1 层；`RandomEnemy` → 兜底随机一个可命中敌人）；其余无目标牌 → 安全跳过。
  - **以后新增「给牌附墨影」的来源或改这两个补丁时，务必保留兜底**。若想改回"同一敌人每次打牌只加 1 层"，在 `InkyHitRegistry.Record` 里加回 `if (!hits.Contains(target))` 去重即可。
