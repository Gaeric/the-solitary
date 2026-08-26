using MegaCrit.Sts2.Core.CardSelection;
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

namespace TheSolitary.Cards;

// 游龙（原附魔灵巧，character.org 白卡 #3）：1 费攻击。
// 造成 6 点伤害，为手牌中一张牌附魔灵巧（Nimble：此牌获得的格挡+X）3。
// 只有能被灵巧附魔的牌（能获得格挡的牌）才能被选择；手牌中没有可用牌时跳过附魔。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class EnchantNimble : ModCardTemplate
{
	// 灵巧（Nimble）附魔层数的 DynamicVar 键名与数值。
	private const string NimbleAmountKey = "NimbleAmount";
	private const int NimbleAmount = 3;

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

	public EnchantNimble()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/EnchantNimble.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：伤害 + 灵巧附魔层数（绑定 {Damage:diff()} 与 {NimbleAmount:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(6m, ValueProp.Move),
		new DynamicVar(NimbleAmountKey, NimbleAmount)
	];

	// 打出时：先造成伤害，再为手牌中一张能附魔灵巧的牌附魔灵巧。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target);

		// 1. 造成 6 点伤害。
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.Execute(choiceContext);

		// 2. 手牌中没有能被灵巧附魔的牌时，直接跳过附魔（不弹选择界面）。
		if (!Owner.PlayerCombatState!.Hand.Cards.Any(CanEnchantNimble))
		{
			return;
		}

		// 3. 选择一张手牌附魔灵巧。filter 只放行能附魔的牌（状态/诅咒等类型会被
		//    CanEnchant 拒绝，已附魔的牌也会被排除），避免 CardCmd.Enchant 因 CanEnchant 失败而抛异常。
		CardModel? target = (await CardSelectCmd.FromHand(
			context: choiceContext,
			player: Owner,
			prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 1),
			filter: CanEnchantNimble,
			source: this)).FirstOrDefault();

		// 取消选择则跳过附魔（伤害已造成）。
		if (target != null)
		{
			CardCmd.Enchant<Nimble>(target, DynamicVars[NimbleAmountKey].BaseValue);
		}
	}

	// 升级：伤害 6 -> 9。
	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(3m);
	}

	/// <summary>
	/// 检查目标牌能否附魔灵巧（Nimble），与 CardCmd.Enchant 内部的 CanEnchant 检查一致。
	/// </summary>
	private static bool CanEnchantNimble(CardModel card)
	{
		return ModelDb.Enchantment<Nimble>().ToMutable().CanEnchant(card);
	}
}
