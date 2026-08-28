using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Enchantments;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 动能回收（Kinetic Recovery，character.org 蓝卡 #15）效果的 Power：
// 拥有者打出的下一张攻击牌获得动量（Momentum）附魔。
// 参考迂回 DetourPower 的「下一张牌」触发模式：BeforeCardPlayed 命中后施加效果，
// 再 PowerCmd.Decrement 扣 1，扣完自动移除。
[RegisterPower]
public sealed class KineticRecoveryPower : ModPowerTemplate
{
	// 施加的动量附魔数值（character.org 未指定，取基础值 1；可按需调整）。
	private const int MomentumAmount = 3;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	// 悬停提示：展示动量附魔的机制（每次打出该攻击牌，攻击伤害永久 +MomentumAmount）。
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		HoverTipFactory.FromEnchantment<Momentum>(MomentumAmount);

	// 拥有者打出攻击牌时：给该牌附魔动量，再扣减剩余次数（1 次后自动移除）。
	// 用 BeforeCardPlayed：动量附魔在本张牌效果结算前挂上，从下一次打出起生效。
	public override async Task BeforeCardPlayed(CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner.Creature != base.Owner || cardPlay.Card.Type != CardType.Attack)
		{
			return;
		}

		Flash();
		CardCmd.Enchant<Momentum>(cardPlay.Card, MomentumAmount);
		await PowerCmd.Decrement(this);
	}
}
