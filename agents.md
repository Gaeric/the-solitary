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
│   ├── Cards/                # 卡牌类（Strike/Defend/交换附魔/减益符/无尽符/撒符等）
│   ├── Powers/               # Power 类（EndlessCharmPower 等）
│   └── Relics/               # 遗物类（TheSolitaryRelic / SwiftCircuit）
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

- **直接可用的 png**：`../WatcherBeautified/images/packed/card_portraits/watcher/*.png`（185 张观者全卡图，如 cataclysm / deva_form / omniscience）。`Watcher/_imported/*.ctex` 是压缩纹理，别直接用。
- 游戏卡框尺寸 **500×380（横向）**；源图若已是 500×380（如 deva_form、omniscience）直接复制；竖版（1058×1487 等）需**居中裁剪到 500:380 再缩放**避免变形：

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

## 关键注意事项（Gotchas）

- **`Entry.cs` 的 `ModId` 必须与 `TheSolitary.json` 的 `id` 一致**（当前都是 `TheSolitary`）。
- `res://TheSolitary/...` 中的 `TheSolitary` 是 **PCK 资源目录名**，不是 C# namespace。
- 构建时会运行 `SyncManifestDependencies`，自动把 `TheSolitary.json` 的 `dependencies[STS2-RitsuLib].version` 同步为实际 NuGet 版本；但 **`min_game_version` 仍需人工核对**（当前 `0.106.0`）。
- 三个 RitsuLib 包（主线 + Compat）**一次只能启用一个**；主线 `STS2.RitsuLib` 仅支持 STS2 0.105.0+。
- `.csproj` 通过 `Krafs.Publicizer` 公开 sts2 内部成员；游戏 dll 从 `$(Sts2DataDir)` 引用（`0Harmony.dll` / `sts2.dll` / `Steamworks.NET.dll`）。
- `local.props`、`.tools/`、`*.uid`、`*.import` 都在 `.gitignore` 中，不要提交。
- 美术资源取材自 `../WatcherBeautified`（观者美化包，GDRE 导出的 Godot 工程），详见「美术资源目录」一节。
- 查阅游戏 API 与原版描述措辞优先参考 `../sts2_20260821`（源码 + 本地化）；旧 `.tools/sts2_decomp/` 仅为残留反编译副本。
