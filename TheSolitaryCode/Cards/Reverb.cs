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

// 余音（character.org 蓝卡 #33，原名附魔守护）：1 费能力牌。
// 每当你生成附魔时，获得 4 点格挡（升级后 6 点）。
// 触发实现见 AfterEnchantPatch（CardCmd.Enchant 补丁）＋ ReverbPower。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Reverb : ModCardTemplate
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
	// PowerVar 键名（与 typeof(ReverbPower).Name 一致，绑定 {ReverbPower:diff()} 占位符）。
	private const string ReverbKey = nameof(ReverbPower);
	// 每次生成附魔获得的格挡（升级后 6）。
	private const int BlockAmount = 4;

	public Reverb()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Reverb.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：每次生成附魔获得 4 点格挡（升级后 6），绑定 {ReverbPower:diff()} 占位符。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<ReverbPower>(BlockAmount)
	];

	// 悬停提示：余音 Power（数量由 Amount 显示）。
	// 注意：HoverTipFactory.FromPower<T>() 返回单个 IHoverTip（不是 IEnumerable），用集合表达式包一层。
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[HoverTipFactory.FromPower<ReverbPower>()];

	// 打出时：给自己施加余音（层数 = 每次生成附魔应获得的格挡）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
		await PowerCmd.Apply<ReverbPower>(choiceContext, Owner.Creature, DynamicVars[ReverbKey].BaseValue, Owner.Creature, this);
	}

	// 升级：格挡 4 -> 6。
	protected override void OnUpgrade()
	{
		DynamicVars[ReverbKey].UpgradeValueBy(2m);
	}
}
