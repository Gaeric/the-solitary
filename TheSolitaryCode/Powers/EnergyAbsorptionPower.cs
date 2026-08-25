using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 元能吸附的 Power（参考环回形态 LoopFormPower 的计数阈值结算模式）：
// 每当拥有者打出一张附魔牌时累计次数，每累计 5 张获得 Amount 点能量后清零。
[RegisterPower]
public sealed class EnergyAbsorptionPower : ModPowerTemplate
{
	// 附魔牌阈值：固定每 5 张附魔牌结算一次能量。
	private const int EnchantedCardsThreshold = 5;

	// 距上次获得能量以来打出的附魔牌数量（每 EnchantedCardsThreshold 张结算一次后清零）。
	private int _enchantedCardsSinceGain;

	private int EnchantedCardsSinceGain
	{
		get
		{
			return _enchantedCardsSinceGain;
		}
		set
		{
			AssertMutable();
			_enchantedCardsSinceGain = value;
		}
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	// 悬停提示自动带上能量图标（显示 Amount 数值）。
	protected override bool IncludeEnergyHoverTip => true;

	// 每当拥有者打出一张附魔牌时触发（参考狂怒 RagePower 的归属与附魔判定）。
	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner != base.Owner.Player)
		{
			return;
		}
		if (cardPlay.Card.Enchantment == null)
		{
			return;
		}

		EnchantedCardsSinceGain++;
		if (EnchantedCardsSinceGain < EnchantedCardsThreshold)
		{
			return;
		}

		// 累计达到阈值张附魔牌：清空计数并发放能量。
		EnchantedCardsSinceGain = 0;
		Flash();
		await PlayerCmd.GainEnergy(base.Amount, base.Owner.Player);
	}
}
