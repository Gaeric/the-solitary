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
│   ├── Cards/                # 卡牌类（TheSolitaryStrike / TheSolitaryDefend）
│   └── Relics/               # 遗物类（TheSolitaryRelic / SwiftCircuit）
├── TheSolitary/              # Godot 资源（images / localization / scenes），非 C#
│   ├── localization/
│   │   ├── eng/              # 英文：cards.json / characters.json / relics.json / ancients.json
│   │   └── zhs/              # 简体中文（同上）
│   ├── images/               # 卡图 / 角色图 / 遗物图
│   └── scenes/characters/    # 角色 tscn 场景
├── TheSolitary.csproj        # MSBuild 工程（Godot.NET.Sdk/4.5.1, net10.0, C# 13）
├── TheSolitary.json          # Mod manifest（id / dependencies / min_game_version）
├── project.godot             # Godot 工程配置
├── local.props(.template)    # 本机路径配置（gitignored）
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
- 查阅游戏 API 优先参考 `../sts2` 源码或 `.tools/sts2_decomp/` 反编译副本。
