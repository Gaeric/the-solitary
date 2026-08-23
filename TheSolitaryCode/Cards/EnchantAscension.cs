using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 附魔升华（character.org 金卡 #4）：1 费技能，升级后 0 费。
// 使牌组中所有附魔数值 +1，本局游戏全局生效（永久附魔跨战斗保留；战斗中的临时附魔仅本场战斗 +1）。
// 实现时区分临时附魔与永久附魔（由 EnchantHelpers.IncreaseEnchantmentValue 的 DeckVersion 判空天然区分）：
// - 永久附魔（牌组中真实存在的附魔）：战斗副本与牌组版本同步 +1，本局游戏永久生效；
// - 临时附魔（本场战斗中才产生的附魔，如战斗中生成的牌上的附魔）：仅本场战斗 +1。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class EnchantAscension : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（金卡 = Rare）。
	private const CardRarity CardRarityValue = CardRarity.Rare;
	// 目标类型（Self：作用于己方整个牌组）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public EnchantAscension()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/EnchantAscension.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 打出时：遍历所有战斗牌堆（手牌/抽牌堆/弃牌堆/消耗堆/打出堆）中的牌，
	// 每张带数值型附魔的牌，其附魔数值 +1。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		foreach (CardModel card in EnchantHelpers.GetAllCombatPileCards(Owner))
		{
			if (EnchantHelpers.HasValueEnchantment(card))
			{
				// persistToDeckVersion: true（默认）——永久附魔同步递增牌组版本 Amount，本局游戏全局生效；
				// 临时附魔（DeckVersion 为空，如战斗中生成的牌）仅本场战斗 +1。
				EnchantHelpers.IncreaseEnchantmentValue(card);
			}
		}
	}

	// 升级：费用 1 -> 0。
	protected override void OnUpgrade()
	{
		base.EnergyCost.UpgradeBy(-1);
	}
}
