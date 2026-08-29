using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 振荡（Oscillation）的临时 Power（蓝卡 振荡 授予，Amount=1 表示持续 1 个敌方回合）：
// 本回合中，敌人每有一种负面效果，对你造成的伤害降低 10%。
// 实现参考原版巨像 ColossusPower：覆写 ModifyDamageMultiplicative 减伤 +
// AfterSideTurnEnd 时 PowerCmd.Decrement 移除。
// 与原版 Colossus 的区别：不限制攻击伤害（去掉 IsPoweredAttack 过滤）——对全部伤害来源生效；
// 且不要求攻击者带特定负面效果，改为按所有存活敌人的负面效果总数线性减伤。
[RegisterPower]
public sealed class OscillationPower : ModPowerTemplate
{
	// 每种负面效果降低的伤害比例（与卡牌描述里 OscillationReduction 占位符的 10 保持一致）。
	private const decimal DamageReductionPerDebuff = 0.10m;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	// 自定义图标（取材 WatcherBeautified 观者 Power 图标，128x128，小图与大图共用）。
	public override PowerAssetProfile AssetProfile => new(
		IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
		BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png");

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
	{
		// 只对"拥有者（玩家）受到伤害"生效。
		if (target != base.Owner)
		{
			return 1m;
		}
		// 统计所有存活敌人身上的负面效果总数（每种负面效果各算一次，与层数无关）。
		int debuffCount = base.Owner.CombatState!.HittableEnemies
			.Sum(e => e.Powers.Count(p => p.Type == PowerType.Debuff));
		if (debuffCount == 0)
		{
			return 1m;
		}
		return Math.Max(0m, 1m - DamageReductionPerDebuff * debuffCount);
	}

	// 敌方回合结束时移除（Amount 递减到 0 自动消失）——"本回合"效果。
	public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		if (side == CombatSide.Enemy)
		{
			await PowerCmd.Decrement(this);
		}
	}
}
