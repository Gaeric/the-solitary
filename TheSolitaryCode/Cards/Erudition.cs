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
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
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

	// 待变换成的能力牌（OnPlay 选中后写入）。
	private ModelId? _pendingPowerId;
	private bool _pendingPowerUpgraded;

	/// <summary>
	/// 本卡要变换成的能力牌 Id。OnPlay 选中后**同时**写在本场战斗的克隆与本卡对应的[gold]牌组原件[/gold]上，
	/// 并用 <c>[SavedProperty]</c> 随存档保存：万一战斗中的变换没能走完（被打断 / 读档），
	/// 牌组原件仍能在 <see cref="AfterCombatEnd"/> 里补做这次变换，保证"学会的能力"不会丢。
	/// 值为 null（无待转换）时不写入存档（<see cref="SerializationCondition.SaveIfNotTypeDefault"/>）。
	/// </summary>
	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public ModelId? PendingPowerId
	{
		get => _pendingPowerId;
		set
		{
			AssertMutable();
			_pendingPowerId = value;
		}
	}

	/// <summary>与 <see cref="PendingPowerId"/> 配套：待变换的能力牌是否应为升级版。</summary>
	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool PendingPowerUpgraded
	{
		get => _pendingPowerUpgraded;
		set
		{
			AssertMutable();
			_pendingPowerUpgraded = value;
		}
	}

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
	/// 打出时：从其它角色的稀有能力牌里随机取 5 张（本卡升级后为升级过的版本）→ 选择 1 张。
	/// 真正的"把这张牌变换成所选能力牌 → 附魔 注能 → 打出变换后的牌"要等本卡的**打出流程结束**
	/// （离开打出区、进入结果牌堆）之后再做，见 <see cref="AfterCardChangedPiles"/>：
	/// 在 OnPlay 里替换正在打出的牌会打断牌堆流程（原版把结果牌堆的搬运放在 OnPlay 之后）。
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

		// 3. 记录选择结果：本场战斗的克隆与本卡对应的**牌组原件**都记一份（[SavedProperty]，随存档保存），
		//    真正的"变换本卡 → 附魔注能 → 打出变换后的牌"等本卡打出流程结束后执行（见 AfterCardChangedPiles）。
		PendingPowerId = chosen.Id;
		PendingPowerUpgraded = chosen.IsUpgraded;
		if (DeckVersion is Erudition deckVersion)
		{
			deckVersion.PendingPowerId = chosen.Id;
			deckVersion.PendingPowerUpgraded = chosen.IsUpgraded;
		}
	}

	/// <summary>
	/// 本卡**打出流程结束**（从打出区进入结果牌堆）后触发，按顺序执行：
	/// ① 把这张牌本身变换为所选能力牌 → ② 附魔 注能 → ③ 把变换后的牌打出去；
	/// ④ 再把[gold]牌组原件[/gold]变换为同一张能力牌并附魔 注能，使"学会的能力"跨战斗永久保留
	/// （注能的效果是"每场战斗开始时自动打出"，只有牌组里的那份才能生效）。
	/// </summary>
	public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
	{
		// 只处理"这张牌自己离开打出区"的那次牌堆变化（其余牌堆变化，如变换后新牌加入牌堆，直接忽略）。
		if (card != this || oldPileType != PileType.Play || PendingPowerId is not { } powerId)
		{
			return;
		}
		bool upgraded = PendingPowerUpgraded;
		PendingPowerId = null;
		PendingPowerUpgraded = false;

		try
		{
			CardModel? canonical = ModelDb.GetByIdOrNull<CardModel>(powerId);
			if (canonical == null)
			{
				return;
			}

			// 变换前先记住牌组原件（变换这张牌后本实例会被移出状态，DeckVersion 就取不到了）。
			CardModel? deckCard = DeckVersion;

			// ① 这张牌本身：在战斗作用域建替换牌 → 变换 → ② 附魔注能 → ③ 打出变换后的牌。
			//    自动打出用 ThrowingPlayerChoiceContext（与原版 PrepTimePower / 本 Mod AfterEnchantPatch 一致）：
			//    能力牌不会要求玩家二次选择。
			CardModel replacement = CreatePowerReplacement(
				CombatState!.CreateCard(canonical, Owner), upgraded);
			CardPileAddResult? selfResult = await CardCmd.Transform(this, replacement);
			if (selfResult?.cardAdded is { } transformedSelf)
			{
				EnchantHelpers.ApplyEnchantmentBypassingCanEnchant(
					transformedSelf, ModelDb.Enchantment<Imbued>().ToMutable());
				await CardCmd.AutoPlay(new ThrowingPlayerChoiceContext(), transformedSelf, null);
			}

			// ④ 牌组原件：同样变换 + 附魔注能（这一步让能力牌永久留在牌组里，下一场战斗自动打出）。
			if (deckCard != null)
			{
				CardPileAddResult? deckResult = await TransformDeckCardToPower(deckCard, canonical, upgraded);
				if (deckResult?.cardAdded is { } transformedDeck)
				{
					EnchantHelpers.ApplyEnchantmentBypassingCanEnchant(
						transformedDeck, ModelDb.Enchantment<Imbued>().ToMutable());
				}
			}
		}
		catch (Exception ex)
		{
			// 这段逻辑挂在"牌堆搬运"的钩子里，异常会打断打出流程，因此自行吞掉并记录。
			Entry.Logger.Error(ex.ToString());
		}
	}

	// 按所选能力牌建一张替换牌，升级状态与所选牌一致（参考 变化 Begone 的"创建替换牌 + CardCmd.Upgrade"写法）。
	private static CardModel CreatePowerReplacement(CardModel replacement, bool upgraded)
	{
		if (upgraded)
		{
			CardCmd.Upgrade(replacement);
		}
		return replacement;
	}

	/// <summary>
	/// 把牌组原件变换为所选能力牌：替换牌必须建在**运行作用域**（IRunState : ICardScope）里——
	/// 战斗中 <see cref="CardModel.CardScope"/> 会优先返回战斗作用域，那样建出来的牌属于本场战斗、
	/// 战斗结束会被清理，无法永久留在牌组里。
	/// </summary>
	/// <returns>变换结果（cardAdded 为真正进入牌组的那张牌，可能被"加入牌组"钩子替换）。</returns>
	private static async Task<CardPileAddResult?> TransformDeckCardToPower(CardModel deckCard, CardModel canonical, bool upgraded)
	{
		// 牌组原件可能已经被本卡本次打出的前一次结算替换掉了（例如带"重放"效果时 OnPlay 会跑多次），
		// 这时它已不在任何牌堆中（RemoveFromState），不能再变换，直接跳过。
		if (deckCard.HasBeenRemovedFromState || deckCard.Pile == null)
		{
			return null;
		}

		CardModel replacement = CreatePowerReplacement(
			deckCard.Owner.RunState.CreateCard(canonical, deckCard.Owner), upgraded);
		CardPileAddResult? result = await CardCmd.Transform(deckCard, replacement);
		if (result?.cardAdded != null && deckCard is Erudition deckErudition)
		{
			// 转换完成：清掉牌组原件上的待转换记录（失败时保留，交给 AfterCombatEnd 兜底重试）。
			deckErudition.PendingPowerId = null;
			deckErudition.PendingPowerUpgraded = false;
		}
		return result;
	}

	/// <summary>
	/// 兜底：如果这张牌在战斗中的变换没能走完（被打断 / 读档恢复），只要它还在[gold]牌组[/gold]里，
	/// 就在战斗结束时补做一次变换 + 附魔 注能（战斗结束时牌组里的牌都处于安全状态）。
	/// 战斗中的克隆此时在战斗牌堆里，不满足条件，会被跳过。
	/// </summary>
	public override async Task AfterCombatEnd(CombatRoom room)
	{
		if (PendingPowerId is not { } powerId || Pile?.Type != PileType.Deck)
		{
			return;
		}

		try
		{
			CardModel? canonical = ModelDb.GetByIdOrNull<CardModel>(powerId);
			if (canonical == null)
			{
				return;
			}

			CardPileAddResult? result = await TransformDeckCardToPower(this, canonical, PendingPowerUpgraded);
			if (result?.cardAdded is { } transformed)
			{
				EnchantHelpers.ApplyEnchantmentBypassingCanEnchant(
					transformed, ModelDb.Enchantment<Imbued>().ToMutable());
			}
		}
		catch (Exception ex)
		{
			Entry.Logger.Error(ex.ToString());
		}
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
