using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 迂回（Detour）效果的 Power（参考原版 FreeSkillPower / FreeAttackPower 的改费机制）：
// 拥有者打出的下一张附魔牌耗能为 0。
// 计数器 = 剩余可免费的附魔牌张数；每打出符合条件的附魔牌，PowerCmd.Decrement 扣 1，扣完自动移除。
[RegisterPower]
public sealed class DetourPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	// 自定义图标（取材 WatcherBeautified 观者 Power 图标，128x128，小图与大图共用）。
	public override PowerAssetProfile AssetProfile => new(
		IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
		BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png");

	// 战斗内计算耗能时：拥有者手牌/打出区的附魔牌耗能改为 0。
	// 与原版 FreeSkillPower 完全同款，仅把“Skill 类型”判定换成“有附魔”判定。
	public override bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (card.Owner.Creature != base.Owner || card.Enchantment == null)
		{
			return false;
		}
		if (card.Pile?.Type is not (PileType.Hand or PileType.Play))
		{
			return false;
		}
		modifiedCost = 0m;
		return true;
	}

	// 打出符合条件的附魔牌时，把剩余免费次数 -1（参考原版 FreeSkillPower.BeforeCardPlayed）。
	public override async Task BeforeCardPlayed(CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner.Creature != base.Owner || cardPlay.Card.Enchantment == null)
		{
			return;
		}
		if (cardPlay.Card.Pile?.Type is not (PileType.Hand or PileType.Play))
		{
			return;
		}
		await PowerCmd.Decrement(this);
	}
}
