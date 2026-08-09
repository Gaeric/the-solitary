# agents.md

> 本文档用于指导 AI 编码代理（Claude Code / Codex 等）在 **TheSolitary**（Slay the Spire 2 Mod）项目上工作。
> 代理在读写本仓库文件时应优先遵守这里的约定。

## Project（项目概述）

**TheSolitary** 是一个基于 [STS2-RitsuLib](https://github.com/BAKAOLC/STS2-RitsuLib) 的 Slay the Spire 2（STS2）Mod，采用 **Godot 4.5.1 + C#（.NET 10 / C# 13）** 开发。

- 入口：`TheSolitaryCode/Entry.cs`，通过 `[ModInitializer]` 注册
- 内容：自定义角色 `TheSolitaryCharacter`（含卡牌池、遗物池、药水池）、4 张初始打击、4 张初始防御、初始遗物
- 内容通过 RitsuLib **自动注册**（`[RegisterCard]` / `[RegisterRelic]` / `[RegisterCharacter]`），无需在入口手动逐个注册

> **关于 Agent/LLM 架构**：经全量搜索，本仓库 **不包含任何 Agent/LLM 架构代码**——没有 LLM 调用、Prompt 模板或多 Agent 协作系统。游戏本体（`../sts2`）中只有 AutoSlay 自动战斗等传统游戏 AI，非 LLM Agent。本文件定位为「指导 AI 编码代理在本项目上工作的 AGENTS.md」。

## STS2 源码目录

- 本仓库：`d:\sts2_mods\the-solitary`
- STS2 游戏源码：**`../sts2`**（绝对路径 `d:\sts2_mods\sts2`）——查游戏 API 时参考这里
- 反编译副本：`.tools/sts2_decomp/`（已在 `.gitignore`，仅供本地查阅）

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
  - **Spine Godot Extension** 放在项目根目录 `bin/`（`spine_godot_extension.gdextension` + `windows/` dll），用**标准 Godot 4.5.1** 导出（`local.props` 的 `GodotExe` 已指回标准版；MegaDot 无法加载该扩展）。游戏自带的 `../sts2/addons/spine` 不要使用（会让编辑器崩溃）。
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
4. `dotnet build .\TheSolitary.csproj` 验证编译通过。

## 关键注意事项（Gotchas）

- **`Entry.cs` 的 `ModId` 必须与 `TheSolitary.json` 的 `id` 一致**（当前都是 `TheSolitary`）。
- `res://TheSolitary/...` 中的 `TheSolitary` 是 **PCK 资源目录名**，不是 C# namespace。
- 构建时会运行 `SyncManifestDependencies`，自动把 `TheSolitary.json` 的 `dependencies[STS2-RitsuLib].version` 同步为实际 NuGet 版本；但 **`min_game_version` 仍需人工核对**（当前 `0.106.0`）。
- 三个 RitsuLib 包（主线 + Compat）**一次只能启用一个**；主线 `STS2.RitsuLib` 仅支持 STS2 0.105.0+。
- `.csproj` 通过 `Krafs.Publicizer` 公开 sts2 内部成员；游戏 dll 从 `$(Sts2DataDir)` 引用（`0Harmony.dll` / `sts2.dll` / `Steamworks.NET.dll`）。
- `local.props`、`.tools/`、`*.uid`、`*.import` 都在 `.gitignore` 中，不要提交。
- 美术资源取材自 `../WatcherBeautified`（观者美化包，GDRE 导出的 Godot 工程），详见「美术资源目录」一节。
- 查阅游戏 API 优先参考 `../sts2` 源码或 `.tools/sts2_decomp/` 反编译副本。
