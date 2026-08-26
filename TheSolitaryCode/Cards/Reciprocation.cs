using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 往复（新白卡）：1 费攻击。
// 造成 8 点伤害；如果这张牌带有附魔，获得 6 点格挡（升级后伤害 10、格挡 8）。
// “是否带附魔”直接读取本次打出实例 cardPlay.Card.Enchantment（CardPlay 暴露被打出的 CardModel，
// Enchantment 属性为 null 表示未附魔）；带附魔时 GainBlock 会完整走 Hook.ModifyBlock 管线，
// 因此灵巧/伶俐等附魔带来的额外格挡也会一并生效。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Reciprocation : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（攻击）。
	private const CardType CardKind = CardType.Attack;
	// 卡牌稀有度（白卡 = Common）。
	private const CardRarity CardRarityValue = CardRarity.Common;
	// 目标类型（任意敌人）。
	private const TargetType CardTarget = TargetType.AnyEnemy;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public Reciprocation()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 条件性获得格挡，按格挡牌参与敏捷等计算（参考熔炼 Smelt）。
	public override bool GainsBlock => true;

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Reciprocation.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：伤害 8 + 条件格挡 6（绑定 {Damage:diff()} / {Block:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(8m, ValueProp.Move),
		new BlockVar(6m, ValueProp.Move)
	];

	// 打出时：先造成伤害；若这张牌带附魔则获得格挡。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target);

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.Execute(choiceContext);

		if (cardPlay.Card.Enchantment != null)
		{
			await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
		}
	}

	// 升级：伤害 8 -> 10，格挡 6 -> 8。
	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(2m);
		DynamicVars.Block.UpgradeValueBy(2m);
	}
}
