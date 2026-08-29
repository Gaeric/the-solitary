using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 聚焦（Focus）的 Power（金卡 聚焦 授予的常驻效果）：
// 敌人每有一种负面效果，受到的所有伤害额外增加 Amount%。
// 实现参考原版 TrackingPower，但去掉 props.IsPoweredAttack() 限制——对全部伤害来源生效
// （攻击/卡牌/能力/遗物伤害都吃加成），且不限目标必须带某种特定负面效果，
// 而是按目标身上的负面效果种类数线性放大：倍率 = 1 + (Amount/100) × 负面效果种类数。
[RegisterPower]
public sealed class FocusPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	// 自定义图标（取材 WatcherBeautified 观者 Power 图标，128x128，小图与大图共用）。
	public override PowerAssetProfile AssetProfile => new(
		IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
		BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png");

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
	{
		// 伤害来源必须是拥有者本人或其宠物（参考原版 TrackingPower）。
		if (dealer != base.Owner && !base.Owner.Pets.Contains(dealer))
		{
			return 1m;
		}
		if (target == null)
		{
			return 1m;
		}
		// 统计目标身上的负面效果种类数（不限于攻击伤害——本卡影响全部伤害来源）。
		int debuffCount = target.Powers.Count(p => p.Type == PowerType.Debuff);
		if (debuffCount == 0)
		{
			return 1m;
		}
		return 1m + (base.Amount / 100m) * debuffCount;
	}
}
