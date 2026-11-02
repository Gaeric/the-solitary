using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using TheSolitary.ArtEffects;

namespace TheSolitary.Cards;

// 混叠（character.org todo 蓝卡）：1 费技能，打出后消耗。
// 随机触发3次术式（升级后为术式+）；为手牌中一张牌附魔本能。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Aliasing : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（蓝卡 = Uncommon）。
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	// 目标类型（自身：生成术式 / 作用于手牌）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public Aliasing()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Aliasing.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 打出后消耗。
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	// 基础数值：术式触发次数（绑定 {Cards:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(3)
	];

	// 打出时：触发随机术式，再为一张手牌附魔本能。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		// 1. 每次独立随机选择术式和敌人，升级由 IsUpgraded 控制。
		for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
		{
			await ArtTrigger.TriggerRandom(Owner, CombatState!, Owner.RunState.Rng.CombatCardGeneration, choiceContext, upgraded: IsUpgraded);
			await Cmd.Wait(0.1f);
		}

		// 2. 手牌中没有能被本能附魔的牌时，跳过附魔（不弹选择界面）。
		if (!Owner.PlayerCombatState!.Hand.Cards.Any(CanEnchantInstinct))
		{
			return;
		}

		// 3. 选择一张手牌附魔本能（filter 只放行能附魔的牌，避免 CardCmd.Enchant 因 CanEnchant 失败而抛异常）。
		CardModel? target = (await CardSelectCmd.FromHand(
			context: choiceContext,
			player: Owner,
			prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 1),
			filter: CanEnchantInstinct,
			source: this)).FirstOrDefault();

		// 取消选择则跳过附魔（术式已结算）。
		if (target != null)
		{
			CardCmd.Enchant<Instinct>(target, 1m);
		}
	}

	// 升级：生成的术式变为升级版（术式+），由 IsUpgraded 控制，无需额外升级逻辑。

	/// <summary>
	/// 检查目标牌能否附魔本能（Instinct），与 CardCmd.Enchant 内部的 CanEnchant 检查一致。
	/// </summary>
	private static bool CanEnchantInstinct(CardModel card)
	{
		return ModelDb.Enchantment<Instinct>().ToMutable().CanEnchant(card);
	}
}
