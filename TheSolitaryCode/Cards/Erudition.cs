using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Enchantments;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 博识（character.org）：0 费技能牌，由任务牌 勤学 转换而来。
// 从 5 张"其它角色"的稀有（金卡）能力牌中选 1 张直接打出；
// 并把这张牌（牌组原件）永久转换为所选能力牌 + 附魔 注能 Imbued（每场战斗开始时自动打出）。
// 衍生牌：Token 稀有度 + 注册进原版 TokenCardPool（与术式/小刀 Shiv 同类），
// 因此不会出现在卡牌奖励、商店、随机变化、战斗生成等任何获取途径中（只能由 勤学 转换得到）。
// 实现参考：原版 富足 Abundance（候选生成 + 选择 + 打出）、变化 Begone（创建替换牌 + CardCmd.Transform +
// 升级判定）、注释 Charge（DynamicVars.Cards 作为候选数量）。
[RegisterCard(typeof(TokenCardPool))]
public sealed class Erudition : ModCardTemplate
{
	// 基础耗能（0 费）。
	private const int BaseEnergyCost = 0;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（Token = 衍生牌，不参与奖励稀有度骰子）。
	private const CardRarity CardRarityValue = CardRarity.Token;
	// 目标类型（Self：效果作用于自己）。
	private const TargetType CardTarget = TargetType.Self;
	// 衍生牌不出现在卡牌图鉴中。
	private const bool ShowInCardLibrary = false;

	public Erudition()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Erudition.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 只能由 勤学 转换获得，不应被"在战斗中生成卡牌"的效果抽到。
	public override bool CanBeGeneratedInCombat => false;

	// 悬停提示：注能附魔。
	// 注意：HoverTipFactory.FromEnchantment<T>() 本身返回 IEnumerable<IHoverTip>，不能再用集合表达式包一层。
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		HoverTipFactory.FromEnchantment<Imbued>();

	// 基础数值：候选张数 5（绑定 {Cards:diff()} 占位符；升级只改变候选是否为升级版，数量不变）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(5)
	];

	/// <summary>
	/// 打出时：从其它角色的稀有能力牌里随机取 5 张（本卡升级后为升级过的版本）→ 选择 1 张直接打出 →
	/// 把这张牌（牌组原件）永久转换为所选能力牌并附魔 注能。
	/// </summary>
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		// 1. 生成候选：其它角色卡池里的稀有（金卡）能力牌，随机取 5 张互不相同的实例。
		List<CardModel> candidates = CardFactory.GetDistinctForCombat(
			Owner,
			RarePowersFromOtherCharacters(),
			DynamicVars.Cards.IntValue,
			Owner.RunState.Rng.CombatCardGeneration).ToList();

		if (candidates.Count == 0)
		{
			return;
		}

		// 升级版：候选全部为升级过的能力牌（与富足 Abundance 升级候选同款做法）。
		if (IsUpgraded)
		{
			foreach (CardModel candidate in candidates)
			{
				CardCmd.Upgrade(candidate);
			}
		}

		// 2. 选择 1 张。
		//    注意：候选有 5 张，超过 CardSelectCmd.FromChooseACardScreen 的 3 张上限（内部 `cards.Count > 3` 即抛异常，
		//    原版 飞溅 Splash 之所以能用是因为它只取 3 张），因此这里用网格选择界面 FromSimpleGrid
		//    （与遗物 抉择悖论 ChoicesParadox 的"多选一"同款，战斗中可用，无张数限制）。
		List<CardModel> selected = (await CardSelectCmd.FromSimpleGrid(
			choiceContext,
			candidates,
			Owner,
			new CardSelectorPrefs(base.SelectionScreenPrompt, 1))).ToList();

		CardModel? chosen = selected.FirstOrDefault();
		if (chosen == null)
		{
			return;
		}

		// 3. 直接打出所选能力牌（自动打出即免费；生成牌不在任何牌堆，AutoPlay 会自行放进打出区）。
		await CardCmd.AutoPlay(choiceContext, chosen, null);

		// 4. 把这张牌永久转换为所选能力牌。
		//    转换对象是牌组原件（战斗克隆的 DeckVersion）——本场战斗的克隆会在战斗结束时丢弃，
		//    只有改牌组原件才能让"学会的能力"跨战斗保留。
		CardModel? deckCard = cardPlay.Card.DeckVersion;
		if (deckCard == null)
		{
			// 这张牌不是来自牌组（例如战斗中复制/生成的实例），无法永久转换，直接结束（不做战斗内临时变换，
			// 以免打断"正在打出的牌"自身的牌堆流程）。
			return;
		}

		CardPileAddResult? transformResult = await TransformDeckCardToPower(deckCard, chosen);
		if (transformResult?.cardAdded is not { } transformed)
		{
			return;
		}

		// 5. 为转换后的能力牌附魔 注能。
		//    注能只允许附魔技能牌（Imbued.CanEnchantCardType），这里绕过 CanEnchant 直接施加。
		EnchantHelpers.ApplyEnchantmentBypassingCanEnchant(
			transformed, ModelDb.Enchantment<Imbued>().ToMutable());
	}

	/// <summary>
	/// 把牌组原件变换为所选能力牌：按 canonical 版本新建一张可变替换牌，升级状态与所选牌保持一致，
	/// 再走 <see cref="CardCmd.Transform"/>。参考 变化 Begone 的"创建替换牌 + CardCmd.Upgrade"写法。
	/// </summary>
	/// <returns>变换结果（cardAdded 为真正进入牌组的那张牌，可能被"加入牌组"钩子替换）。</returns>
	private static async Task<CardPileAddResult?> TransformDeckCardToPower(CardModel deckCard, CardModel chosen)
	{
		// CreateCard 需要 canonical 实例（内部会 ToMutable），所选牌是战斗中的可变实例，故按 Id 取回原型。
		CardModel? canonical = ModelDb.GetByIdOrNull<CardModel>(chosen.Id);
		if (canonical == null)
		{
			return null;
		}

		// 替换牌必须建在**运行作用域**（IRunState : ICardScope）里：战斗中 CardModel.CardScope 会优先返回
		// 战斗作用域（CardModel.CardScope => CombatState ?? ... ?? RunState），那样建出来的牌属于本场战斗、
		// 战斗结束会被清理，无法永久留在牌组里。这里显式用 RunState 新建运行作用域的牌。
		CardModel replacement = deckCard.Owner.RunState.CreateCard(canonical, deckCard.Owner);
		if (chosen.IsUpgraded)
		{
			CardCmd.Upgrade(replacement);
		}

		return await CardCmd.Transform(deckCard, replacement);
	}

	/// <summary>
	/// "其它角色"的稀有（金卡）能力牌：遍历所有角色卡池，排除自己所属的角色，
	/// 按稀有度（Rare = 金卡）与类型（Power = 能力）过滤。
	/// </summary>
	private IEnumerable<CardModel> RarePowersFromOtherCharacters()
	{
		// 用类型比较而不是实例比较：Owner.Character 是可变实例，ModelDb.AllCharacters 是 canonical 实例。
		Type ownCharacterType = Owner.Character.GetType();

		return ModelDb.AllCharacters
			.Where(character => character.GetType() != ownCharacterType)
			.SelectMany(character => character.CardPool.GetUnlockedCards(
				Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint))
			.Where(card => card.Type == CardType.Power && card.Rarity == CardRarity.Rare);
	}
}
