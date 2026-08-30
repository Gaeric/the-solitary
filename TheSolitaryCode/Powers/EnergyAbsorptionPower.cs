using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 元能吸附的 Power（参考原版神气制胜 PanachePower / 环绕轨道 OrbitPower 的进度提示方案）：
// 每当拥有者打出一张附魔牌时累计次数，每累计 5 张获得 Amount 点能量后清零。
// 图标上的数字 = 距下次触发还差几张附魔牌；悬停提示通过 {EnchantedCardsLeft} 占位符实时显示剩余张数。
[RegisterPower]
public sealed class EnergyAbsorptionPower : ModPowerTemplate
{
	// 附魔牌阈值：固定每 5 张附魔牌结算一次能量。
	private const int EnchantedCardsThreshold = 5;

	// 剩余附魔牌数的 DynamicVar 键名（与 powers.json smartDescription 中的 {EnchantedCardsLeft} 占位符对应）。
	private const string EnchantedCardsLeftKey = "EnchantedCardsLeft";

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	// 自定义图标（取材 WatcherBeautified 观者 Power 图标，128x128，小图与大图共用）。
	public override PowerAssetProfile AssetProfile => new(
		IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
		BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png");

	// 悬停提示自动带上能量图标（显示 Amount 数值）。
	protected override bool IncludeEnergyHoverTip => true;

	// 图标上的数字 = 距下次触发还差几张附魔牌（参考神气制胜 PanachePower 的 DisplayAmount 覆写）。
	public override int DisplayAmount => base.DynamicVars[EnchantedCardsLeftKey].IntValue;

	// 进度变量：每打出 1 张附魔牌减 1，归零触发后重置为阈值；自动绑定 smartDescription 的 {EnchantedCardsLeft} 占位符。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar(EnchantedCardsLeftKey, EnchantedCardsThreshold)
	];

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

		base.DynamicVars[EnchantedCardsLeftKey].BaseValue--;
		InvokeDisplayAmountChanged();
		if (base.DynamicVars[EnchantedCardsLeftKey].IntValue > 0)
		{
			return;
		}

		// 累计达到阈值张附魔牌：重置进度并发放能量。
		base.DynamicVars[EnchantedCardsLeftKey].BaseValue = EnchantedCardsThreshold;
		InvokeDisplayAmountChanged();
		Flash();
		await PlayerCmd.GainEnergy(base.Amount, base.Owner.Player);
	}
}
