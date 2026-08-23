using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 附魔壁垒（character.org 蓝卡 #12）：1 费技能，防御终端。
// 你每有一张附魔牌，获得 2 点格挡（升级后每张 3 点）。
// 注意：每张附魔牌逐次获得格挡，敏捷/灵巧作用于每张的 2/3 上（而非最终总数），
// 战斗内预览由 PerCardCalculatedBlockVar 保证与实际行为一致。
// 附魔牌数量统计与附魔风暴 EnchantStorm 共用 EnchantHelpers，避免两处逻辑漂移。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class EnchantedBulwark : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（蓝卡 = Uncommon）。
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	// 目标类型（Self：作用于己方）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public EnchantedBulwark()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 获得格挡，按格挡牌参与力量/敏捷等计算。
	public override bool GainsBlock => true;

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/EnchantedBulwark.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：格挡 = 附魔牌数量 × 每张附魔牌的格挡值。
	// CalculationBase = 0（无基础格挡），CalculationExtra = 每张附魔牌的格挡值（2，升级后 3）。
	// OnPlay 按每张附魔牌逐次 GainBlock，敏捷/灵巧作用于每张的 2/3 上（而非最终总数），
	// 因此用 PerCardCalculatedBlockVar 让战斗内预览与实际行为一致。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CalculationBaseVar(0m),
		new CalculationExtraVar(2m),
		new PerCardCalculatedBlockVar(ValueProp.Move, static (card, _) =>
			EnchantHelpers.CountEnchantedCardsInAllPiles(card.Owner))
	];

	// 打出时：每张附魔牌单独获得格挡，使敏捷/灵巧作用于每张的 2/3 上，而不是最终总数。
	// fast: true 避免连续多次格挡的动画过慢（参考原版 AfterimagePower 逐次 GainBlock）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		int count = EnchantHelpers.CountEnchantedCardsInAllPiles(Owner);
		for (int i = 0; i < count; i++)
		{
			await CreatureCmd.GainBlock(
				Owner.Creature,
				DynamicVars.CalculationExtra.BaseValue,
				DynamicVars.CalculatedBlock.Props,
				cardPlay,
				fast: true);
		}
	}

	// 升级：每张附魔牌获得的格挡 2 -> 3。
	protected override void OnUpgrade()
	{
		DynamicVars.CalculationExtra.UpgradeValueBy(1m);
	}
}
