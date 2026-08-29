using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 抽样（Sampling）的 Power（金卡 抽样 授予的一次性效果，参考原版药水 复制 DuplicationPower）：
// 拥有者打出的下一张（层数 Amount 张）附魔牌被打出两次。
// ModifyCardPlayCount 返回 +1 次打出次数，AfterModifyingCardPlayCount 里把剩余次数 -1，
// 扣完（Amount 归零）自动移除；不限定回合，未消耗则持续保留（与迂回 DetourPower 一致）。
[RegisterPower]
public sealed class SamplingPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	// 战斗内计算打出次数时：拥有者打出的附魔牌打出次数 +1（参考复制 DuplicationPower.ModifyCardPlayCount）。
	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		if (card.Owner.Creature != base.Owner || card.Enchantment == null)
		{
			return playCount;
		}
		return playCount + 1;
	}

	// 打出次数结算完毕后，把剩余免费次数 -1（参考复制 DuplicationPower.AfterModifyingCardPlayCount）。
	public override async Task AfterModifyingCardPlayCount(CardModel card)
	{
		if (card.Owner.Creature != base.Owner || card.Enchantment == null)
		{
			return;
		}
		await PowerCmd.Decrement(this);
	}
}
