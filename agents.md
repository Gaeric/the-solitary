# agents.md

> 面向在 TheSolitary 仓库中工作的 AI 编码代理。本文记录当前开发约定与关键约束，不作为教程或变更日志。

## 项目与事实来源

TheSolitary 是基于 STS2-RitsuLib 的 Slay the Spire 2 角色 Mod，使用 **Godot.NET.Sdk 4.5.1、.NET 10、C# 13**。核心机制为附魔、独立术式触发及牌组运转，不再是最小示例模板。

- 仓库根目录：`d:\sts2_mods\the-solitary`。
- 入口：`d:\sts2_mods\the-solitary\TheSolitaryCode\Entry.cs`。内容通过 RitsuLib 特性自动注册；Harmony 补丁、自定义奖励恢复工厂和原版事件映射仍需显式注册。
- 初始牌组：4 打击、4 防御、唤醒 `Sacrifice`、匣中术 `ArtOfTheBox`、勤学 `DiligentStudy`、超渡 `Requiem`；初始遗物为 `SwiftCircuit`。以源码注册特性为准，不在本文重复平衡数值。
- 游戏源码与原版本地化：`d:\sts2_mods\sts2_20260821`，优先查其 `src\Core` 和 `localization\eng`、`localization\zhs`。安装版 DLL 若与快照不同，应核对实际编译依赖，必要时反编译确认。
- 美术源工程：`d:\sts2_mods\WatcherBeautified`，是观者美化包导出的 Godot 工程。
- 术式迁移与回归清单：`d:\sts2_mods\the-solitary\docs\art-trigger-migration.md`。
- `d:\sts2_mods\the-solitary\character.org` 是设计记录，可能含旧方案；与实现不符时先说明差异，不擅自修改设计文档或按旧方案回退实现。
- README、manifest 中仍有模板遗留描述；API、行为和构建配置以当前源码、工程文件及实际依赖为准。不要把历史版本快照当作最新版本。

## 目录职责

以下路径均相对于 `d:\sts2_mods\the-solitary`：

| 路径 | 职责 |
| --- | --- |
| `TheSolitaryCode/Entry.cs` | 初始化、补丁、奖励恢复工厂与原版事件映射 |
| `TheSolitaryCode/Cards/` | 卡牌模型、卡牌专用预览与负面效果工具 |
| `TheSolitaryCode/Arts/` | 术式定义、触发、监听、特效；命名空间为 `TheSolitary.ArtEffects` |
| `TheSolitaryCode/Enchantments/` | 跨卡牌、能力、遗物复用的附魔工具与随机池 |
| `TheSolitaryCode/Powers/` | 能力模型与事件响应 |
| `TheSolitaryCode/Characters/` | 角色模型、内容池与 Godot 包装类型 |
| `TheSolitaryCode/Relics/` | 遗物模型 |
| `TheSolitaryCode/Rewards/` | 自定义奖励及其恢复逻辑 |
| `TheSolitaryCode/Patches/` | 原版行为兼容、附魔状态记录补丁 |
| `TheSolitary/` | Godot 资源与中英本地化，不放 C# 逻辑 |
| `animations/characters/watcher/` | 顶层 Spine 骨骼资源，路径不可随意移动 |
| `bin/` | 本机 Spine Godot Extension，已忽略但导出必需，不可当普通构建缓存清空 |
| `docs/` | 专项机制说明、迁移与回归记录 |

## 修改与简化原则

1. 先读相关实现、调用方、本地化和构建配置，再决定修改范围；不要根据文件名或旧注释猜行为。
2. 优先删除重复判断、无用包装和复述代码的注释，不以减少行数为目标改变机制。
3. 注释使用中文，解释事件顺序、存档、随机数同步、作用域、原版限制等原因；不重复说明常量名、卡面效果或逐行执行过程。
4. 复用已有工具；卡牌特有的类型限制留在调用方。不要为少量重复引入通用框架、额外依赖或层层包装。
5. 内容模型保持既有类名和命名空间；移动或改名之前检查注册 ID、存档、本地化和资源引用。不能因没有普通调用引用就删除自动注册模型或兼容类型。
6. 不顺手更改卡牌数值、随机源、结算顺序、叠加方式或存档语义。必要变更应说明兼容影响并补回归检查。
7. 不覆盖用户已有改动。完成后检查 Git 差异，明确哪些验证已执行、哪些仍需游戏内测试。

## 代码与内容约定

- 使用 file-scoped namespace，内容类通常为 `public sealed class`；沿用周边文件排版，保持 UTF-8。
- 卡牌配置使用既有 `const` 字段模式；基础数值通过 `CanonicalVars` 和 `DynamicVar` 声明，效果放在 `OnPlay`，升级放在 `OnUpgrade`。
- 异步结算使用游戏命令并按顺序 `await`；不要把需要等待的效果变成无人等待的异步调用。
- 普通资源路径使用 `$"{Entry.ResPath}/images/.../{GetType().Name}.png"` 等仓库内路径；顶层 Spine 路径是明确例外。
- 新增卡牌、遗物、能力分别使用 `[RegisterCard]`、`[RegisterRelic]`、`[RegisterPower]`。初始内容另加对应 `RegisterCharacterStarter...` 特性，不在入口逐个手动注册内容模型。
- 按定位选择卡池：一般角色卡进 `TheSolitaryCardPool`，任务牌进原版 `QuestCardPool`，衍生牌进原版 `TokenCardPool`。衍生牌通常隐藏图鉴并禁止战斗随机生成，参考现有实现，不把普通奖励牌误放入 Token 池。
- 事件钩子先检查归属和触发条件，再 `Flash()` 和结算；涉及生成牌或附魔的连锁时检查重入风险，不添加无实际用途的递归守卫。
- 每个副本独立计数时使用 `PowerInstanceType.Instanced`，参考 `EnchantedCreationPower`、`ArcaneWeavingPower`、`EnergyAbsorptionPower`；不要改成共享 `Amount` 叠加。
- 展示进度放入 `DynamicVars`；自定义角标进度改变时按现有实现调用 `InvokeDisplayAmountChanged()`。私有运行数据可用 `InitInternalData` / `GetInternalData<T>`。
- 监听 `AfterCardPlayed` 的新能力可能看到施加它的那张牌；是否跳过须按具体效果判断，不能把首次跳过逻辑复制到所有钩子中。

### 本地化与 API 易错点

- 新增或修改玩家可见文本，同时更新 `d:\sts2_mods\the-solitary\TheSolitary\localization\eng` 和 `zhs` 对应 JSON。
- 键名沿用大写下划线格式，如 `THE_SOLITARY_CARD_ART_OF_THE_BOX.*`、`THE_SOLITARY_POWER_ENCHANTED_CREATION_POWER.*`，不能直接把 PascalCase 类名拼入键中。
- 描述与 `smartDescription` 的占位符必须匹配 `CanonicalVars`；消耗、固有等自动显示的关键词不在描述中重复。
- 术式用“触发 / trigger”，不要再写成“生成卡牌”或“打出术式牌”。其他措辞优先对照原版本地化与相邻内容。
- `CardModel` 位于 `MegaCrit.Sts2.Core.Models`。
- RitsuLib 卡牌/能力模板的 `ExtraHoverTips` 为 sealed，扩展使用 `AdditionalHoverTips`；能量提示可按现有能力使用 `IncludeEnergyHoverTip`。
- `PowerVar<T>` 的变量键为 `typeof(T).Name`，例如 `DynamicVars["SlowPower"]`；不要假设所有变量都有快捷访问器。
- 不盲目用 `!` 消除可空警告；先确认战斗、所有者和牌堆在调用上下文中确实存在。

## 核心机制约束

### 独立术式

实现位于 `d:\sts2_mods\the-solitary\TheSolitaryCode\Arts`：

- 统一通过 `ArtTrigger.Trigger` / `TriggerRandom` 结算，数值与表现定义在 `ArtDefinition`。新来源不要另造术式卡生成或自动打出路径。
- 不创建 `CardModel`、不操作牌堆、不伪造生成牌、打牌、消耗或充能球事件。使用 `ValueProp.Unpowered`，伤害来源 `dealer` / `cardSource` / `cardPlay` 均为空。
- 玩家归属用于选敌和专用联动，不作为伤害来源。负面效果也没有 applier / cardSource；先伤害后施加，致死时不继续施加。正常伤害事件仍会执行，“无来源”不等于绕过伤害管线。
- 无可命中敌人或战斗结束时停止；默认随机选敌，显式目标必须有效。所有可命中敌人均已有缓慢时，随机池排除凋零。
- `ArtTrackerPower` 在每次实际伤害结算前计数。直接联动实现 `IArtTriggerListener`，自行检查 `trigger.Owner`；致死的该次触发仍通知快照中的监听者。
- `SpellRecallPower` 的额外结算不递归扩大、不重抽种类；每次实际结算都计数并通知，原目标死亡后重新选敌。
- 五张旧 Token：`ArtOfDecay`、`ArtOfWilt`、`ArtOfBreach`、`ArtOfVenom`、`ArtOfDoom` 暂留兼容旧存档与资源，已无生成来源。未经明确兼容策略，不删除或重新接入它们。
- 特效与逻辑分离，保留 TestMode / NonInteractiveMode 下跳过表现的处理。修改后检查附魔/打牌计数不被术式误触发。

### 附魔工具与恢复

实现位于 `d:\sts2_mods\the-solitary\TheSolitaryCode\Enchantments`：

- 可用性检查复用 `EnchantHelpers.CanEnchant<T>`；随机附魔复用 `RandomEnchantPool`，不在各来源复制池子或数值。
- 生成牌随机附魔由 `DimensionalHarmonyPower` 负责，不能沿用旧文档把它归给万物通元。检查生成者、卡牌归属、已有附魔及重入状态。
- `EnchantedCreationPower` 监听术式，为手牌数值型附魔加值且仅本场战斗有效，必须传 `persistToDeckVersion: false`。该参数默认会同步牌组版本，使用时须明确永久/临时语义。
- 原版清除附魔不会撤销 `OnEnchant` 对费用和关键词的改写。移除/交换必须复用工具中的恢复流程，不能直接调用 `CardCmd.ClearEnchantment`。
- 替换顺序固定为 **Capture 旧副作用 → Clear → Restore 原卡 → Apply 新附魔**。先恢复再施加，否则新附魔添加的关键词可能被误删。
- `TezcatarasEmberCostRecordPatch` 记录余烬前费用，`EnchantKeywordRecordPatch` 记录附魔前本地关键词；关键词按快照还原，不可无条件移除 `Eternal`，以免删除卡牌自带词条。
- 交换前重建双方附魔为全新实例，使一次性运行状态复位；不重建卡牌，以保留卡牌本场战斗状态。
- 附魔 `Props` 中的快照不会随序列化重建保留，补丁须在每次 `ModifyCard` 时重新记录；不要把它当跨存档持久数据。
- 对外复用公开方法 `SwapEnchantmentsBetweenTwoHandCards`、`SwapEnchantmentWithHandCard`、`ShuffleEnchantmentsInCards`。`ReplaceEnchantment`、双牌直接交换及副作用快照目前为内部私有实现，新增用途需在工具内合理扩展，而非在调用方绕过恢复。
- 融会贯通的重排语义：多张牌使用错排，单张牌重新充能，空集合无事发生，不能替换成允许保留自身位置的普通洗牌。
- `ApplyEnchantmentBypassingCanEnchant` 仅用于效果明确允许绕过类型限制的场景，不用作普通附魔入口。

### 任务、牌组与存档

- 勤学与超渡为初始任务牌：`Quest` 类型/稀有度、费用 -1、不可打出、不可升级、不可战斗生成，注册进 `QuestCardPool`。不要为了让它们参与基础牌变换而擅自改为 Basic。
- **勤学当前是五种房型各进入两次，奖励一个稀有遗物和一个罕见遗物后移出牌组，不再转换为博识。** `Erudition` 类仍存在；不要因旧文档或残留注释恢复旧关联，也不要未经兼容检查删除它。
- 超渡只在 `AfterCombatEnd` 统计 `RoomType.Monster`，精英和首领不算；不能用进入战斗代替完成战斗。
- 任务进度只对 `PileType.Deck` 原件计数，避免战斗克隆重复推进。持久进度用 `[SavedProperty]`，setter 同步显示变量。
- 更改保存属性的编码语义时，应显式迁移或换名；不能沿用旧名把旧数据误读成新格式。
- 战斗克隆通过 `DeckVersion` 对应牌组原件。战斗内永久转换牌组时，在 `RunState` 作用域创建替换牌，而非优先落到战斗作用域的 `CardScope`；后续操作使用转换结果的 `cardAdded`。
- 不在 `OnPlay` 内直接破坏正在执行的打牌搬运流程；复杂转换参考 `Erudition` 的延迟转换及存档兜底，并检查重放、取消、中断和读档。
- 自定义奖励使用 `ModCustomReward` 并在入口注册恢复工厂。参考 `RandomEnchantmentReward`：取消选择保留奖励，无候选可正常完成，永久附魔作用于牌组原件。
- 随机选择使用游戏同步选择命令和语义对应的 RNG；例如凝华奖励使用玩家 `Rewards` RNG。不要改用 `System.Random` 或随意更换随机流。

### Harmony 补丁与墨影

- 新增 `IPatchMethod` 后，必须在入口通过 `CreatePatcher`、`RegisterPatch<T>`、`PatchAll()` 显式注册；`PatchId` 保持唯一。
- 目标方法尽量显式指定完整参数类型，避免重载歧义；兼容性失败的处理沿用现有补丁，不默认把所有补丁设为关键补丁。
- Prefix 改写参数时使用原参数名和 `ref`。跳过返回 `Task` 的原方法时，必须设置非空 `__result`；需要异步结算就返回对应任务供原流程等待。
- 不新引入无人等待的 `async void` 效果；现有此类后置补丁的异常必须自行捕获并记录。
- 墨影依赖两个补丁：`InkyHitRecordPatch` 逐命中记录，`InkyTargetPatch` 在伤害结算后按记录施加虚弱。保留重复命中，不按敌人去重。
- 保留无命中时的目标兜底：指定目标/全体目标走原逻辑，随机敌人牌补一个有效目标，其他无目标情况安全跳过；不能恢复为直接解引用空目标。
- 本项目既有 Harmony 独立实验使用 net9.0；所用游戏 Harmony 曾无法在 CoreCLR 10 下运行。不要把 Mod 的 net10.0 编译成功等同于独立补丁实验可在同一运行时执行，需先核实安装版运行环境。

## 资源与 Spine

- 新素材优先从 `d:\sts2_mods\WatcherBeautified` 选取，复制到仓库内再引用，不直接引用仓库外绝对路径。素材版权归源作者，发布前确认授权。
- 卡图 PNG 池：`d:\sts2_mods\WatcherBeautified\images\packed\card_portraits\watcher`。并非只有 `.ctex`；不要把压缩纹理直接作为普通 PNG 肖像路径。
- 普通卡图沿用项目 **500×380 横向**规格；不同宽高比先等比裁剪再缩放，不硬拉伸。
- 先古卡图使用 **606×852 竖向**规格，仍通过 `PortraitPath` 设置。先古布局直接拉满矩形，不能使用普通横向图，也不应为此改 `VisualStyle`。
- 已有先古卡图：`Revelation` 使用 `draw_talisman`，`Resurgence` 使用 `preach`。新选图检查实际占用及视觉区分度，不能只比较文件大小；重编码后同图大小可能不同，应用缩略像素特征并人工查看。
- 不复制源工程的 `.import`、`.uid` 或 `.godot`。替换图片后检查并移除对应旧 `.png.import`，让 Godot 重新导入；卡图变化需导出 PCK 才能进游戏。
- 战斗场景当前使用 `Sprite2D` 和 `TheSolitary/images/characters/TheSolitary_combat.png` 静态立绘；坐垫和核心暂合在同一张图中。修改大小时同步核对 `Bounds`、`CenterPos` 和提示位置，不沿用直立人物的中心点。`ref/` 是原画参考目录，不参与 PCK 导出。
- 选人、营火和商店仍使用观者 Spine 模型。导出使用支持本机扩展的**标准 Godot 4.5.1 .NET 版**，本机配置以 `local.props` 为准；模板里残留的 MegaDot 路径不可直接照用。
- 扩展位于 `d:\sts2_mods\the-solitary\bin\spine_godot_extension.gdextension` 及其 `windows` DLL 目录；已被 Git 忽略，克隆后需自行准备。不要改用游戏源码中的 Spine 插件。
- 骨骼保持顶层 `res://animations/characters/watcher/`，当前扩展对嵌套 `res://TheSolitary/...` 的路径处理存在问题。
- 角色仍保留观者的 `SetupCustomCreatureAnimator` 映射 `Idle` / `Attack` / `Cast` / `Hit` / `Dead` / `relaxed`，静态战斗场景不会启用该映射。重新接入 Spine 时先核对骨骼动画名，不直接改成游戏标准名。

## 构建、验证与交付

### 本机配置

首次使用时复制 `d:\sts2_mods\the-solitary\local.props.template` 为同目录 `local.props`，填写：

- `Sts2Dir`：游戏安装目录。
- `Sts2DataDir`：游戏 DLL 目录，通常为 `$(Sts2Dir)/data_sts2_windows_x86_64`。
- `GodotExe`：上述标准 Godot .NET 可执行文件，仅导出 PCK 需要。
- `RitsuLibDeployDir`：可选，默认游戏 `mods/STS2-RitsuLib`。

`local.props`、`.tools/`、`.godot/`、`bin/`、`obj/`、`*.uid`、`*.import` 和日志为本机/生成内容，不提交。

### 命令与副作用

仅编译、不复制当前 Mod、不导出 PCK：

```powershell
dotnet build 'd:\sts2_mods\the-solitary\TheSolitary.csproj' /p:RunPckExport=false /p:CopyModOnBuild=false
```

这仍是普通构建，会运行 manifest 依赖同步；RitsuLib 包自身也有部署逻辑，`CopyModOnBuild=false` 只控制当前 Mod 的 CopyMod 目标。仅检查 C# 且需跳过本工程 manifest 同步时，可使用已有还原结果的设计时验证：

```powershell
dotnet build 'd:\sts2_mods\the-solitary\TheSolitary.csproj' --no-restore /p:RunPckExport=false /p:CopyModOnBuild=false /p:DesignTimeBuild=true
```

设计时验证不能替代正常构建、部署及游戏测试；没有还原结果时先按环境完成依赖还原。

完整构建、部署 DLL/manifest 并导出 PCK：

```powershell
dotnet build 'd:\sts2_mods\the-solitary\TheSolitary.csproj'
```

- 默认部署到游戏 `mods/TheSolitary`。更改资源时需要完整构建；跳过 PCK 不更新游戏内图片、场景或本地化。
- RitsuLib 使用浮动 NuGet 版本，实际版本以还原结果为准。普通构建的 `SyncManifestDependencies` 会同步 manifest 的 RitsuLib 依赖；`min_game_version` 仍须人工核对，不自动改动或宣称兼容。
- `Entry.ModId` 必须与 manifest `id` 一致；PCK 中的 `TheSolitary` 是资源目录名，不是 C# 命名空间。
- 游戏引用由 `Sts2DataDir` 提供，内部成员通过 Krafs.Publicizer 公开；查 API 时不要另引未经确认的框架或库。

### 验证要求

- 当前没有自动化单元测试或独立 lint 工具链。代码变更至少编译验证，资源变更需 PCK 导出及游戏内确认。
- 文档变更核对源码、路径、命令与链接，不必为纯文字修改重新部署游戏。
- 机制变更按涉及范围检查：多人归属与随机同步、无目标/战斗结束、多段及重放、独立能力计数、战斗克隆与牌组原件、存档/读档、取消选择、附魔交换后的费用及关键词恢复。
- 启动日志检查补丁实际应用情况；编译通过不代表 Harmony 已注册或签名匹配。
- 术式相关变更同时核对迁移文档中的间接联动与表现回归项，不能把尚未实测的项目标成通过。
- 交付前检查 `git diff --check` 和 `git status --short`，防止构建同步 manifest 或生成文件造成意外改动；报告实际验证结果和未验证项。
- 后续更新本文优先修正事实、约束与定位入口，不追加大段示例、一次性排查脚本、平衡数值表或历史流水账。