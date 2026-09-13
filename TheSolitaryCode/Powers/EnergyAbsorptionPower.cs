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

// 元能吸附的 Power（进度提示方案与斗转星移 LoopFormPower 一致，并参考原版神气制胜 PanachePower 的独立计数）：
// 每当拥有者打出一张附魔牌时累计次数，每累计 5 张获得 Amount 点能量后清零。
// 图标上的数字 = 距下次触发还差几张附魔牌；悬停提示通过 {EnchantedCardsLeft} 占位符实时显示剩余张数。
// 与斗转星移不同的是本能力使用 InstanceType.Instanced：
// 每打出一次元能吸附都会新建一个独立实例，每一个「开的」元能吸附单独计数、单独显示剩余张数，
// 多个副本各自累计、各自结算，互不影响（原版神气制胜 PanachePower / 定时炸弹 TheBombPower 同款写法）。
[RegisterPower]
public sealed class EnergyAbsorptionPower : ModPowerTemplate
{
	// 附魔牌阈值：固定每 5 张附魔牌结算一次能量。
	private const int EnchantedCardsThreshold = 5;

	// 剩余附魔牌数的 DynamicVar 键名（与 powers.json smartDescription 中的 {EnchantedCardsLeft} 占位符对应）。
	private const string EnchantedCardsLeftKey = "EnchantedCardsLeft";

	// 每个实例的私有状态（参考原版神气制胜 PanachePower 的 Data）：只用于跳过「施加本能力的这张牌」自己。
	private sealed class Data
	{
		// 施加本能力的卡牌刚被打出、AfterCardPlayed 首次触发时置位。
		public bool AlreadyApplied;
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	// 每次施加都新建独立实例（而非叠加层数）：每一个开的元能吸附单独计数。
	public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

	// 自定义图标（取材 WatcherBeautified 观者 Power 图标，128x128，小图与大图共用）。
	public override PowerAssetProfile AssetProfile => new(
		IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
		BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png");

	// 悬停提示自动带上能量图标（显示 Amount 数值）。
	protected override bool IncludeEnergyHoverTip => true;

	// 图标上的数字 = 本副本距下次触发还差几张附魔牌（参考神气制胜 PanachePower 的 DisplayAmount 覆写）。
	public override int DisplayAmount => base.DynamicVars[EnchantedCardsLeftKey].IntValue;

	// 进度变量：每打出 1 张附魔牌减 1，归零触发后重置为阈值；自动绑定 smartDescription 的 {EnchantedCardsLeft} 占位符。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar(EnchantedCardsLeftKey, EnchantedCardsThreshold)
	];

	// 每个实例各持一份私有数据（每次 Clone/施加都会重新初始化）。
	protected override object? InitInternalData()
	{
		return new Data();
	}

	// 每当拥有者打出一张附魔牌时触发（参考狂怒 RagePower 的归属与附魔判定）。
	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner != base.Owner.Player || cardPlay.Card.Enchantment == null)
		{
			return;
		}

		// 施加本能力的这张牌自身不计入本副本进度：
		// 本副本是在该牌 OnPlay 期间创建的，而 AfterCardPlayed 紧随其后才触发（参考 PanachePower 的 alreadyApplied）。
		Data data = GetInternalData<Data>();
		if (!data.AlreadyApplied)
		{
			data.AlreadyApplied = true;
			return;
		}

		DynamicVar cardsLeft = base.DynamicVars[EnchantedCardsLeftKey];
		cardsLeft.BaseValue--;
		InvokeDisplayAmountChanged();
		if (cardsLeft.IntValue > 0)
		{
			return;
		}

		// 本副本累计达到阈值张附魔牌：重置自身进度并发放能量。
		cardsLeft.BaseValue = EnchantedCardsThreshold;
		InvokeDisplayAmountChanged();
		Flash();
		await PlayerCmd.GainEnergy(base.Amount, base.Owner.Player);
	}
}
