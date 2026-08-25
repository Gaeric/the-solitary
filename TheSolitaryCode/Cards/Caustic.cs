using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 焦散：7 费金卡（稀有）技能牌，目标为所有敌人，打出后消耗。
// 所有敌人失去 2 点力量、2 点敏捷（升级后各 3 点）。
// 每当你生成一张术式时，此牌耗能减少 1（参考火箭飞拳 RocketPunch 的 EnergyCost.AddUntilPlayed 减费机制）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Caustic : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 7;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（金卡 = Rare）。
	private const CardRarity CardRarityValue = CardRarity.Rare;
	// 目标类型（所有敌人，无手动选敌）。
	private const TargetType CardTarget = TargetType.AllEnemies;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public Caustic()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Caustic.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 打出后消耗（关键词自动显示在卡面，不必写进描述）。
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	// 悬停提示：力量 / 敏捷。
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>(),
		HoverTipFactory.FromPower<DexterityPower>()
	];

	// 基础数值：力量流失 / 敏捷流失（升级后各 3）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<StrengthPower>(2m),
		new PowerVar<DexterityPower>(2m)
	];

	// 每当你生成一张术式时，此牌耗能减少 1（参考火箭飞拳 RocketPunch：
	// EnergyCost.AddUntilPlayed 使减费持续到本牌打出为止，打出后重置）。
	public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
	{
		if (creator != base.Owner)
		{
			return Task.CompletedTask;
		}
		if (card.Owner != base.Owner)
		{
			return Task.CompletedTask;
		}
		if (!Arts.IsArt(card))
		{
			return Task.CompletedTask;
		}
		base.EnergyCost.AddUntilPlayed(-1);
		return Task.CompletedTask;
	}

	// 打出时：所有敌人失去力量与敏捷（参考萎靡 Malaise 以负值施加 StrengthPower；
	// 全体施加参考凝滞 Stagnation 遍历 CombatState.HittableEnemies）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		decimal strengthLoss = DynamicVars.Strength.BaseValue;
		decimal dexterityLoss = DynamicVars.Dexterity.BaseValue;
		foreach (Creature enemy in CombatState!.HittableEnemies)
		{
			await PowerCmd.Apply<StrengthPower>(choiceContext, enemy, -strengthLoss, Owner.Creature, this);
			await PowerCmd.Apply<DexterityPower>(choiceContext, enemy, -dexterityLoss, Owner.Creature, this);
		}
	}

	// 升级：力量流失 / 敏捷流失 2 -> 3。
	protected override void OnUpgrade()
	{
		DynamicVars.Strength.UpgradeValueBy(1m);
		DynamicVars.Dexterity.UpgradeValueBy(1m);
	}
}
