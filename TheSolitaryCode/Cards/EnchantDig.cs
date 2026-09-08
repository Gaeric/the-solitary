using System.Collections.Generic;
using System.Linq;
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

// 挖掘（character.org 白卡）：1 费攻击。
// 造成 6 点伤害（升级后 8 点），抽 1 张牌（升级后 2 张）；每抽到一张附魔牌，随机打出一张术式。
// 随机术式生成与快速自动打出复用 Arts.CreateRandomInHandAndFastPlay（参考路径追踪 PathTracing）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class EnchantDig : ModCardTemplate
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

	public EnchantDig()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/EnchantDig.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：伤害 6（升级后 8）+ 抽牌数 1（升级后 2），绑定 {Damage:diff()} / {Cards:diff()} 占位符。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(6m, ValueProp.Move),
		new CardsVar(1)
	];

	// 打出时：造成伤害；抽 N 张牌；每抽到一张附魔牌，随机打出一张术式。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target);

		// 1. 造成伤害。
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.Execute(choiceContext);

		// 2. 抽牌并记录本次实际抽到的卡牌列表（Draw 返回抽到的牌，自动处理洗牌）。
		List<CardModel> drawn = (await CardPileCmd.Draw(
			choiceContext, DynamicVars.Cards.BaseValue, Owner)).ToList();

		// 3. 每抽到一张附魔牌，生成一张随机术式（未升级版）并自动打出（随机敌方目标，参考路径追踪）。
		foreach (CardModel card in drawn.Where(c => c.Enchantment != null))
		{
			await Arts.CreateRandomInHandAndFastPlay(
				Owner, CombatState!, Owner.RunState.Rng.CombatCardGeneration, choiceContext);
		}
	}

	// 升级：伤害 6 -> 8，抽牌数 1 -> 2。
	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(5m);
		DynamicVars.Cards.UpgradeValueBy(1m);
	}
}
