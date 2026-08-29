using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 滤波（Filtering）的 Power（蓝卡 滤波 的能力牌效果）：
// 每当拥有者打出一张附魔牌时累计次数，每累计 3 次额外抽 1 张牌后清零。
// 计数阈值结算模式参考环回形态 LoopFormPower / 元能吸附 EnergyAbsorptionPower。
[RegisterPower]
public sealed class FilterPower : ModPowerTemplate
{
	// 附魔牌触发阈值：固定每 3 张附魔牌结算一次抽牌。
	private const int EnchantedCardsThreshold = 3;

	// 距上次抽牌以来打出的附魔牌数量（每 EnchantedCardsThreshold 张结算一次后清零）。
	private int _enchantedCardsSinceDraw;

	private int EnchantedCardsSinceDraw
	{
		get
		{
			return _enchantedCardsSinceDraw;
		}
		set
		{
			AssertMutable();
			_enchantedCardsSinceDraw = value;
		}
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	// 每当拥有者打出一张附魔牌时触发（参考元能吸附 EnergyAbsorptionPower 的归属与附魔判定）。
	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner != base.Owner.Player || cardPlay.Card.Enchantment == null)
		{
			return;
		}

		EnchantedCardsSinceDraw++;
		if (EnchantedCardsSinceDraw < EnchantedCardsThreshold)
		{
			return;
		}

		// 累计达到阈值张附魔牌：清空计数并额外抽 1 张牌。
		EnchantedCardsSinceDraw = 0;
		Flash();
		await CardPileCmd.Draw(choiceContext, 1, base.Owner.Player);
	}
}
