using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using TheSolitary.Enchantments;

namespace TheSolitary.Cards;

// 有备无患（原名附魔抽牌，character.org 蓝卡 #14）：1 费技能，打出后消耗。
// 抽两张牌，为抽到的每张牌附魔伶俐（Adroit：打出时获得 3 点格挡；数值参考遗物 Kifuda）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Preparedness : ModCardTemplate
{
	// 伶俐（Adroit）附魔层数的 DynamicVar 键名与数值（参考 Kifuda 的 Adroit 3）。
	private const string AdroitAmountKey = "AdroitAmount";
	private const int AdroitAmount = 3;

	private const int BaseEnergyCost = 1;
	private const CardType CardKind = CardType.Skill;
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	private const TargetType CardTarget = TargetType.Self;
	private const bool ShowInCardLibrary = true;

	public Preparedness()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 打出后消耗（基础与升级版一致，关键词自动显示在卡面，不必写进描述）。
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	// 基础数值：抽牌数 + 附魔层数（绑定 {Cards:diff()} 与 {AdroitAmount:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(2),
		new DynamicVar(AdroitAmountKey, AdroitAmount)
	];

	// 打出时：抽牌，为抽到的每张牌附魔伶俐。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		IEnumerable<CardModel> drawn = await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

		// 只附魔能被伶俐附魔的牌：状态/诅咒等类型会被 CanEnchant 拒绝，已附魔的牌也会被排除，
		// 避免 CardCmd.Enchant 因 CanEnchant 失败而抛异常。
		foreach (CardModel card in drawn.Where(EnchantHelpers.CanEnchant<Adroit>))
		{
			CardCmd.Enchant<Adroit>(card, DynamicVars[AdroitAmountKey].BaseValue);
		}
	}

	// 升级：抽 2 -> 3 张牌（消耗与伶俐数值不变）。
	protected override void OnUpgrade()
	{
		DynamicVars.Cards.UpgradeValueBy(1);
	}
}
