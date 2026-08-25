using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using TheSolitary.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 迂回（character.org 蓝卡 #9）：2 费技能。
// 获得 12 点格挡（升级后 16），并使你的下一张附魔牌耗能为 0（升级后仍为一张，只提升格挡）。
// 改费机制参考原版 Pounce / Synthesis / Unrelenting 授予的 Free 系 Power（FreeSkillPower 等）：
// Power 端用 TryModifyEnergyCostInCombatLate 把符合条件的附魔牌耗能改为 0，
// 打出后再在 BeforeCardPlayed 里 PowerCmd.Decrement 扣除剩余次数。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Detour : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 2;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（蓝卡 = Uncommon）。
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	// 目标类型（自身）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;
	// 免费附魔牌张数（固定 1 张，升级不增加）。
	private const int FreeCards = 1;

	public Detour()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 获得格挡，按格挡牌参与力量/敏捷等计算。
	public override bool GainsBlock => true;

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Detour.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：格挡 12（升级后 16），绑定 {Block:diff()} 占位符。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(12m, ValueProp.Move)
	];

	// 打出时：获得格挡，再授予“下一张附魔牌耗能为 0”的 Power（层数固定 1）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
		await PowerCmd.Apply<DetourPower>(choiceContext, Owner.Creature, FreeCards, Owner.Creature, this);
	}

	// 升级：格挡 12 -> 16（免费附魔牌张数保持 1）。
	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(4m);
	}
}
