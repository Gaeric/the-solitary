using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using TheSolitary.Enchantments;

namespace TheSolitary.Cards;

// 压制（character.org todo 白卡）：1 费攻击，打出后消耗。
// 造成 6 点伤害（升级后 9 点）；如果敌人有 3 种以上（≥3）负面效果，
// 为手牌中所有技能牌附魔伶俐（Adroit）3。
// 负面效果类型统计参考瘟疫 Pestilence（DebuffHelpers.CountDebuffTypes）；
// 伶俐数值与有备无患 Preparedness 一致（3，参考遗物 Kifuda）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Suppression : ModCardTemplate
{
	// 伶俐（Adroit）附魔层数的 DynamicVar 键名与数值（参考有备无患 Preparedness / 遗物 Kifuda 的 Adroit 3）。
	private const string AdroitAmountKey = "AdroitAmount";
	private const int AdroitAmount = 3;

	private const int BaseEnergyCost = 1;
	private const CardType CardKind = CardType.Attack;
	private const CardRarity CardRarityValue = CardRarity.Common;
	private const TargetType CardTarget = TargetType.AnyEnemy;
	private const bool ShowInCardLibrary = true;

	public Suppression()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	// 基础数值：伤害 + 伶俐附魔层数（绑定 {Damage:diff()} 与 {AdroitAmount:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(6m, ValueProp.Move),
		new DynamicVar(AdroitAmountKey, AdroitAmount)
	];

	// 打出时：造成伤害；目标有 3 种以上负面效果时为手牌中所有技能牌附魔伶俐。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target);

		// 先快照目标负面效果类型数量（造成伤害前，避免敌人被击杀后无法判断）。
		int debuffTypes = DebuffHelpers.CountDebuffTypes(cardPlay.Target);

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.Execute(choiceContext);

		// 目标负面效果类型不足 3 种时，无事发生。
		if (debuffTypes < 3)
		{
			return;
		}

		// 为手牌中所有能附魔伶俐的技能牌附魔伶俐（状态/诅咒等会被 CanEnchant 拒绝）。
		foreach (CardModel card in Owner.PlayerCombatState!.Hand.Cards
			.Where(c => c.Type == CardType.Skill && EnchantHelpers.CanEnchant<Adroit>(c))
			.ToList())
		{
			CardCmd.Enchant<Adroit>(card, DynamicVars[AdroitAmountKey].BaseValue);
		}
	}

	// 升级：伤害 6 -> 9。
	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(3m);
	}
}
