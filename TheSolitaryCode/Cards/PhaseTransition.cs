using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 相变（character.org todo 白卡）：1 费攻击。
// 造成 6 点伤害；如果目标至少有 2 种负面效果，随机为 1 张手牌附魔（升级后随机为 2 张手牌附魔）。
// 负面效果类型统计参考瘟疫 Pestilence（DebuffHelpers.CountDebuffTypes）；
// 随机附魔复用 RandomEnchantPool（锋利/动量/本能/涡旋/伶俐/灵巧）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class PhaseTransition : ModCardTemplate
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

	public PhaseTransition()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/PhaseTransition.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：伤害 + 随机附魔手牌张数（绑定 {Damage:diff()} / {Cards:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(6m, ValueProp.Move),
		new CardsVar(1)
	];

	// 打出时：造成伤害；目标至少有 2 种负面效果时随机为手牌附魔。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target);

		// 先快照目标负面效果类型数量（造成伤害前，避免敌人被击杀后无法判断）。
		int debuffTypes = DebuffHelpers.CountDebuffTypes(cardPlay.Target);

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.Execute(choiceContext);

		// 目标负面效果类型不足 2 种时，无事发生。
		if (debuffTypes < 2)
		{
			return;
		}

		// 从手牌中筛选出能被随机附魔池作用的牌，随机挑 N 张依次随机附魔。
		List<CardModel> eligible = Owner.PlayerCombatState!.Hand.Cards
			.Where(RandomEnchantPool.CanEnchantRandomly)
			.ToList();
		Rng rng = Owner.RunState.Rng.CombatCardGeneration;
		for (int i = 0; i < DynamicVars.Cards.IntValue && eligible.Count > 0; i++)
		{
			CardModel pick = rng.NextItem(eligible)!;
			RandomEnchantPool.EnchantRandomly(rng, pick);
			eligible.Remove(pick);
		}
	}

	// 升级：随机附魔手牌 1 -> 2 张。
	protected override void OnUpgrade()
	{
		DynamicVars.Cards.UpgradeValueBy(1);
	}
}
