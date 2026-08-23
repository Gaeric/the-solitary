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

// 熔炼（character.org 白卡 #16）：1 费技能。
// 选择一张手牌消耗。获得格挡；若该牌有附魔，则额外再获得一次同数值格挡（升级后基础格挡 +3）。
// 数值与描述参考邪眼 EvilEye：额外格挡 = 再次调用 GainBlock（而非固定值），
// 使两次格挡都完整经过 Hook.ModifyBlock，额外数值同样受敏捷/灵巧/脆弱等修正影响。
// 选择交互参考唤醒 Sacrifice（CardSelectCmd.FromHand + ExhaustSelectionPrompt）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Smelt : ModCardTemplate
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

	public Smelt()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 获得格挡，按格挡牌参与力量/敏捷等计算。
	public override bool GainsBlock => true;

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Smelt.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：基础格挡 8（绑定 {Block:diff()} 占位符）。额外格挡与基础格挡同值，见 OnPlay。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(8m, ValueProp.Move)
	];

	// 打出时：选择一张手牌消耗；获得格挡；若该牌带附魔则额外获得格挡。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		// 1. 选择一张手牌消耗。取消选择则整个效果不发（不消耗、不获得格挡）。
		CardModel? exhausted = (await CardSelectCmd.FromHand(
			prefs: new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1),
			context: choiceContext,
			player: Owner,
			filter: null,
			source: this)).FirstOrDefault();

		if (exhausted == null)
		{
			return;
		}

		// 先记录该牌是否带附魔，再消耗。
		bool wasEnchanted = exhausted.Enchantment != null;

		await CardCmd.Exhaust(choiceContext, exhausted);

		// 2. 参考邪眼 EvilEye：根据条件决定格挡次数，每次都传 DynamicVars.Block 完整走一次 GainBlock，
		//    每次都会经过 Hook.ModifyBlock，因此额外格挡同样受敏捷/灵巧/脆弱等修正影响。
		int blockGains = wasEnchanted ? 2 : 1;
		for (int i = 0; i < blockGains; i++)
		{
			await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
		}
	}

	// 升级：基础格挡 8 -> 11（额外格挡随基础格挡一同提升）。
	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(3m);
	}
}
