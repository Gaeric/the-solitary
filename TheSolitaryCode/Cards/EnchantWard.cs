using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheSolitary.Characters;
using TheSolitary.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 附魔守护（character.org 蓝卡 #33）：1 费能力牌。
// 每当你生成附魔时，获得 6 点格挡（升级后 9 点）。
// 触发实现见 AfterEnchantPatch（CardCmd.Enchant 补丁）＋ EnchantWardPower。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class EnchantWard : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（能力）。
	private const CardType CardKind = CardType.Power;
	// 卡牌稀有度（蓝卡 = Uncommon）。
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	// 目标类型（自身）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;
	// PowerVar 键名（与 typeof(EnchantWardPower).Name 一致，绑定 {EnchantWardPower:diff()} 占位符）。
	private const string WardKey = nameof(EnchantWardPower);
	// 每次生成附魔获得的格挡（升级后 9）。
	private const int BlockAmount = 6;

	public EnchantWard()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/EnchantWard.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：每次生成附魔获得 6 点格挡（升级后 9），绑定 {EnchantWardPower:diff()} 占位符。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<EnchantWardPower>(BlockAmount)
	];

	// 悬停提示：附魔守护 Power（数量由 Amount 显示）。
	// 注意：HoverTipFactory.FromPower<T>() 返回单个 IHoverTip（不是 IEnumerable），用集合表达式包一层。
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[HoverTipFactory.FromPower<EnchantWardPower>()];

	// 打出时：给自己施加附魔守护（层数 = 每次生成附魔应获得的格挡）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
		await PowerCmd.Apply<EnchantWardPower>(choiceContext, Owner.Creature, DynamicVars[WardKey].BaseValue, Owner.Creature, this);
	}

	// 升级：格挡 6 -> 9。
	protected override void OnUpgrade()
	{
		DynamicVars[WardKey].UpgradeValueBy(3m);
	}
}
