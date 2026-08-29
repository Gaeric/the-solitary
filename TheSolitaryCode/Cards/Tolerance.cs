using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 容差（character.org todo 蓝卡）：1 费技能，打出后消耗。
// 消耗手牌中所有未附魔的牌；每消耗一张，获得 1 点能量。升级后获得保留。
// 批量消耗参考原版 第二口气 SecondWind（遍历手牌 + CardCmd.Exhaust）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Tolerance : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（蓝卡 = Uncommon）。
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	// 目标类型（自身：作用于手牌）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public Tolerance()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Tolerance.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 打出后消耗。
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	// 基础数值：每消耗一张获得的能量（绑定 {Energy:energyIcons()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new EnergyVar(1)
	];

	// 打出时：消耗手牌中所有未附魔的牌，每张获得 1 点能量。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		// 先快照手牌中未附魔的牌，避免边遍历边修改集合。
		foreach (CardModel card in PileType.Hand.GetPile(Owner).Cards.Where(c => c.Enchantment == null).ToList())
		{
			await CardCmd.Exhaust(choiceContext, card);
			await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
		}
	}

	// 升级：获得保留。
	protected override void OnUpgrade()
	{
		AddKeyword(CardKeyword.Retain);
	}
}
