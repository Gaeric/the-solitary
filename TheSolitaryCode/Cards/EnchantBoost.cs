using MegaCrit.Sts2.Core.CardSelection;
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

// 附魔增幅（character.org 白卡 #15）：1 费技能。
// 获得 9 点格挡（升级后 12 点）。选择手牌中一张带数值型附魔的牌，本场战斗其附魔数值 +1。
// 实现参考余烬庇护 EmberShelter（获得格挡 + 从手牌选牌）＋ EnchantHelpers.IncreaseEnchantmentValue
// （数值型附魔判断与递增逻辑的共享抽象）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class EnchantBoost : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（白卡 = Common）。
	private const CardRarity CardRarityValue = CardRarity.Common;
	// 目标类型（Self：只作用于己方手牌）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public EnchantBoost()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 获得格挡，按格挡牌参与力量/敏捷等计算。
	public override bool GainsBlock => true;

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/EnchantBoost.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：格挡 9（升级后 12），绑定 {Block:diff()} 占位符。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(9m, ValueProp.Move)
	];

	// 打出时：先获得格挡，再从手牌选一张带数值型附魔的牌，本场战斗其附魔数值 +1。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		// 1. 获得 9 点格挡。
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

		// 2. 手牌中没有带数值型附魔的牌时，直接跳过（不弹选择界面）。
		if (!Owner.PlayerCombatState!.Hand.Cards.Any(EnchantHelpers.HasValueEnchantment))
		{
			return;
		}

		// 3. 选择一张带数值型附魔的手牌。filter 只放行数值型附魔的牌（ShowAmount 为 true 的附魔：
		//    伶俐/动量/灵巧/锋利/迅速/活力），避免选到涡旋/荣光等无 Amount 语义的附魔，+1 无任何效果。
		CardModel? target = (await CardSelectCmd.FromHand(
			context: choiceContext,
			player: Owner,
			prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 1),
			filter: EnchantHelpers.HasValueEnchantment,
			source: this)).FirstOrDefault();

		if (target != null)
		{
			// 本场战斗其附魔数值 +1（persistToDeckVersion: false，不永久改变牌组版本）。
			EnchantHelpers.IncreaseEnchantmentValue(target, persistToDeckVersion: false);
		}
	}

	// 升级：格挡 9 -> 12。
	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(3m);
	}
}
