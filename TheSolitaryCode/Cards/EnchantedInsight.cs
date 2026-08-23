using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 附魔洞察（character.org 蓝卡 #31）：1 费技能。
// 抽 3 张牌；每抽到一张附魔牌，获得 5 点格挡（升级后 8 点）。
// 实现参考附魔抽牌 EnchantedDraw（CardPileCmd.Draw 抽牌）＋ 附魔壁垒 EnchantedBulwark（格挡施加）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class EnchantedInsight : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（蓝卡 = Uncommon）。
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	// 目标类型（Self：作用于己方抽牌堆）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;
	// 每张附魔牌的格挡值 DynamicVar 键名（绑定 {BlockPerEnchanted:diff()} 占位符）。
	private const string BlockPerEnchantedKey = "BlockPerEnchanted";
	// 抽牌数。
	private const int DrawAmount = 3;
	// 每抽到一张附魔牌获得的格挡（升级后 8）。
	private const int BlockPerEnchanted = 5;

	public EnchantedInsight()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 获得格挡，按格挡牌参与力量/敏捷等计算。
	public override bool GainsBlock => true;

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/EnchantedInsight.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：抽牌数 3 + 每张附魔牌格挡 5（绑定 {Cards:diff()} 与 {BlockPerEnchanted:diff()}）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(DrawAmount),
		new DynamicVar(BlockPerEnchantedKey, BlockPerEnchanted)
	];

	// 打出时：抽 3 张牌，按抽到的附魔牌数量一次获得总格挡（每张 5 点）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		// 1. 抽 3 张牌。
		IEnumerable<CardModel> drawn = await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

		// 2. 统计抽到的附魔牌数量；为 0 时跳过格挡（不会获得格挡）。
		int enchantedDrawn = drawn.Count(card => card.Enchantment != null);
		if (enchantedDrawn > 0)
		{
			await CreatureCmd.GainBlock(
				Owner.Creature,
				DynamicVars[BlockPerEnchantedKey].BaseValue * enchantedDrawn,
				ValueProp.Move,
				cardPlay);
		}
	}

	// 升级：每张附魔牌获得的格挡 5 -> 8。
	protected override void OnUpgrade()
	{
		DynamicVars[BlockPerEnchantedKey].UpgradeValueBy(3m);
	}
}
