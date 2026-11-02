# 术式独立触发：第一阶段

## 当前状态

新增独立入口 `TheSolitary.ArtEffects.ArtTrigger.Trigger` / `TriggerRandom`。
本阶段不修改其它卡牌、Power、遗物、本地化或用户正在编辑的 character.org。
**旧 Cards.Arts 与五张 Token 术式仍保留；现有卡牌尚未切换到新入口。**
因此这不是整个卡池迁移完成的版本，游戏中的原有来源仍会生成/打出术式卡牌。

新入口不创建 CardModel、不操作牌堆、不触发生成牌、打牌、消耗或充能球事件。
伤害使用 `ValueProp.Unpowered`，`dealer` / `cardSource` / `cardPlay` 全部为 null。
负面效果同样没有 applier / cardSource，先伤害后施加；致死不再施加。
普通伤害事件和目标受伤结算仍会正常执行；“无来源”不意味着绕过游戏伤害管线。
不受力量、卡牌附魔、卡牌重放或充能球集中加成；不自动推进缓慢的打牌计数。

默认随机选敌，支持显式指定可命中敌人；无敌人不结算并返回 null。
随机术式保留旧规则：所有可命中敌人都有缓慢时，排除凋零。
返回 ArtTriggerResult（归属、种类、是否升级、目标），供后续专用联动接入。
暂不复用 ArtTrackerPower：它现在仍代表旧的“生成牌数量”，不能混计。

## 当前数值（普通 / 升级，沿用代码，不采用旧设计文档中不同的数字）

| 术式 | 伤害 | 负面效果 | 颜色 |
| --- | --- | --- | --- |
| 凋零 Decay | 2 / 3 | 缓慢 1 / 1 | 冰蓝 #65D9FF |
| 枯萎 Wilt | 2 / 3 | 虚弱 1 / 2 | 黄绿 #B8DF60 |
| 破袭 Breach | 2 / 3 | 易伤 1 / 2 | 橙 #FF9860 |
| 浸毒 Venom | 1 / 2 | 中毒 2 / 3 | 翠绿 #45EE96 |
| 灾引 Doom | 2 / 3 | 灾厄 3 / 5 | 紫 #CC7DFF |

## 特效

复用游戏 `res://scenes/vfx/vfx_attack_lightning.tscn`，不复制充能球逻辑。
纹理先去除原黄色，再按术式颜色染色；普通缩放 1.0，升级缩放 1.35。
同种普通与升级版颜色相同，只以缩放区分；颜色和倍率为首版可调参数。
保留原版动画的约 0.33 秒自动释放，使用原闪电激发音效。
无额外逐次等待；TestMode / NonInteractiveMode 跳过表现。
仍需游戏内检查颜色区分、连续触发节奏和不同敌人体型下的视觉效果。

## 待逐项迁移（本阶段不修改）

### 术式来源

- 匣中术 ArtOfTheBox：加入手牌 → 触发，保留升级参数。
- 掌中奇术 ArtOfThePalm：同上，独立评估其自身减费机制。
- 混叠 Aliasing：同上，另保留原卡的附魔效果。
- 能元妙术 ArtOfTheSource / Power：回合开始生成术式+ → 触发术式+。
- 路径追踪 PathTracing：逐张自动打出 → 逐次触发。
- 挖掘 EnchantDig：抽到附魔牌后自动打出 → 触发。
- 全知形态 ArcaneReturn / Power：附魔牌打出后触发术式+，复核并移除不再需要的卡牌递归守卫。
- 来回往复 BackAndForth（遗物）：洗牌生成术式 → 触发。

### 直接监听术式的效果

- 余弦 Cosine / Power：生成牌钩子 → 专用术式触发通知。
- 焦散 Caustic：生成术式减费 → 触发术式减费。
- 术式回想 SpellRecall / Power：不再能给术式卡牌添加 Replay，需另行确认新效果。
- 奇点爆破 SingularityBurst / ArtTrackerPower：生成数 → 触发数，统一统计时机。
- 凝术御甲 ArcaneAegis（遗物）：打出术式获得格挡 → 术式触发联动。
- 术元交感 ArcaneSympathy（遗物）：每打出4张 → 每触发4次，效果待确认。
- 四象之阵 FourSymbolsFormation（遗物）：按 CardModel 类型识别 → 按 ArtKind 识别。

### 已确认重设计，尚未实施

- 凝华 EnchantAscension：金能力，1→0费；打出后在战后提供选牌随机永久附魔奖励，参考禁忌魔典奖励流程。
- 万物通元（现 EnchantedCreation）：金能力，1费，升级固有；每触发5次术式，随机手牌附魔数值+1，仅本场战斗。
- 维度调和（新卡）：蓝能力，2→1费；保留原生成牌随机附魔效果。
- 术元编织（新卡）：蓝能力，2→1费；每触发5次术式，生成一张随机无色牌加入手牌。

### 间接受影响，后续回归检查

元能吸附 EnergyAbsorption、滤波 Filter、光栅 Raster、辐照 Irradiation、元能核心 ArcaneCore、
抽样 Sampling，以及其它按附魔/打牌/攻击次数结算的效果。
它们不应因术式触发而收到伪造的卡牌事件，但仍应响应真正打出的主体卡牌。
最后再删除五张旧 Token 与旧 Cards.Arts，更新中英文描述及悬停提示。