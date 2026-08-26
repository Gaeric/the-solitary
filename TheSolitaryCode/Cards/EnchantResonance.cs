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

// 共鸣（原附魔共鸣，character.org 蓝卡 #27）：1 费能力牌。
// 每当你获得附魔时，获得 3 点活力（升级后 4 点；活力=下次攻击伤害+X）。
// 触发实现见 EnchantResonancePatch（CardCmd.Enchant 补丁）＋ EnchantResonancePower。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class EnchantResonance : ModCardTemplate
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
	// PowerVar 键名（与 typeof(EnchantResonancePower).Name 一致，绑定 {EnchantResonancePower:diff()} 占位符）。
	private const string ResonanceKey = nameof(EnchantResonancePower);
	// 每次获得附魔获得的活力（升级后 4）。
	private const int VigorAmount = 3;

	public EnchantResonance()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/EnchantResonance.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：每次获得附魔获得 3 点活力（升级后 4），绑定 {EnchantResonancePower:diff()} 占位符。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<EnchantResonancePower>(VigorAmount)
	];

	// 悬停提示：附魔共鸣 Power（数量由 Amount 显示）。
	// 注意：HoverTipFactory.FromPower<T>() 返回单个 IHoverTip（不是 IEnumerable），用集合表达式包一层。
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[HoverTipFactory.FromPower<EnchantResonancePower>()];

	// 打出时：给自己施加附魔共鸣（层数 = 每次获得附魔应获得的活力）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
		await PowerCmd.Apply<EnchantResonancePower>(choiceContext, Owner.Creature, DynamicVars[ResonanceKey].BaseValue, Owner.Creature, this);
	}

	// 升级：活力 3 -> 4。
	protected override void OnUpgrade()
	{
		DynamicVars[ResonanceKey].UpgradeValueBy(1m);
	}
}
