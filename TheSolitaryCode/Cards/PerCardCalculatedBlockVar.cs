using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheSolitary.Cards;

/// <summary>
/// 按每张牌单独计算格挡的 <see cref="CalculatedBlockVar"/>。
/// 用于「每有一张附魔牌获得 X 点格挡」这类效果：敏捷/灵巧应作用于每张的 X 上（OnPlay 逐张 GainBlock），
/// 因此战斗内预览把 <see cref="Hook.ModifyBlock"/> 应用在每张的 X 上再乘以张数，与实际行为保持一致。
/// </summary>
public sealed class PerCardCalculatedBlockVar : CalculatedBlockVar
{
	private readonly Func<CardModel, Creature?, decimal> _countCards;

	public PerCardCalculatedBlockVar(ValueProp props, Func<CardModel, Creature?, decimal> countCards)
		: base(props)
	{
		// 复用基类倍率机制：Calculate()/ToString() 等仍按 基础 + 每张值 × 张数 计算。
		WithMultiplier(countCards);
		_countCards = countCards;
	}

	public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
	{
		EnchantmentModel? enchantment = card.Enchantment;
		if (enchantment != null)
		{
			decimal baseValue = GetBaseVar().BaseValue;
			baseValue += enchantment.EnchantBlockAdditive(baseValue);
			baseValue *= enchantment.EnchantBlockMultiplicative(baseValue);
			if (card.IsEnchantmentPreview)
			{
				base.PreviewValue = baseValue;
				return;
			}
			base.EnchantedValue = baseValue;
		}

		// 只在战斗内计算张数：与基类 CalculatedVar.Calculate 的倍率保护一致
		// （CombatManager.Instance.IsInProgress && cardModel.CombatState != null），
		// 非战斗（牌库界面等）时按 0 处理并回退到基础计算，避免在预览阶段
		// 访问战斗牌堆抛 "Tried to get X pile while out of combat."。
		int count = card.CombatState != null ? (int)_countCards(card, target) : 0;
		if (count > 0 && runGlobalHooks)
		{
			// 每张牌单独应用修改（敏捷/灵巧/牌自身的附魔），再乘以张数。
			decimal perCard = GetExtraVar().BaseValue;
			decimal modifiedPerCard = Hook.ModifyBlock(card.CombatState!, card.Owner.Creature, perCard, Props, card, null, out _);
			base.PreviewValue = modifiedPerCard * count;
			return;
		}

		// 非战斗预览或没有附魔牌时，回退到普通计算（基础 + 每张值 × 张数）。
		base.PreviewValue = Calculate(target);
	}
}
