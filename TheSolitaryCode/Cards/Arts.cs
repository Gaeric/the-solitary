using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Random;
using TheSolitary.Powers;

namespace TheSolitary.Cards;

// 术式：TheSolitary 的衍生牌集合（类似小刀），由其它卡牌/遗物生成后加入手牌。
// 共五种：术式-凋零（缓慢）、术式-枯萎（虚弱）、术式-破袭（易伤）、术式-浸毒（中毒）、术式-灾引（灾厄）。
public static class Arts
{
	// 五种术式的原型（canonical）实例，从 ModelDb 中获取。
	private static readonly CardModel[] All =
	[
		ModelDb.Card<ArtOfDecay>(),
		ModelDb.Card<ArtOfWilt>(),
		ModelDb.Card<ArtOfBreach>(),
		ModelDb.Card<ArtOfVenom>(),
		ModelDb.Card<ArtOfDoom>()
	];

	/// <summary>
	/// 生成一张随机的术式并加入手中（等价于 Shiv.CreateInHand 的模式）。
	/// 术式-凋零只有在"至少一个可被攻击的敌人还没有缓慢效果"时才可能被选中，
	/// 避免在全员都已有缓慢时生成一张几乎无效的术式-凋零。
	/// 同时会给拥有者叠加一层隐藏的 ArtTrackerPower，记录本场战斗生成的术式总数。
	/// </summary>
	/// <param name="owner">持有者玩家。</param>
	/// <param name="combatState">当前战斗状态。</param>
	/// <param name="rng">用于随机挑选术式的 RNG（建议传入 RunState.Rng.CombatCardGeneration）。</param>
	/// <param name="choiceContext">用于施加计数器 Power 的选择上下文。</param>
	/// <param name="creator">生成来源（用于统计/来源记录，缺省为 owner）。</param>
	/// <param name="upgraded">是否生成升级版术式（术式+），参考 Largesse / ManifestAuthority 的升级判定模式。</param>
	public static async Task<CardModel> CreateRandomInHand(Player owner, ICombatState combatState, Rng rng, PlayerChoiceContext choiceContext, Player? creator = null, bool upgraded = false)
	{
		// 只要存在一个可命中敌人没有缓慢，就保留术式-凋零作为候选；否则排除它。
		bool anyHittableEnemyWithoutSlow = combatState.HittableEnemies.Any(e => !e.HasPower<SlowPower>());
		IEnumerable<CardModel> candidates = anyHittableEnemyWithoutSlow
			? All
			: All.Where(c => c != ModelDb.Card<ArtOfDecay>());

		CardModel canonical = rng.NextItem(candidates)!;
		CardModel card = combatState.CreateCard(canonical, owner);
		// 升级后的术式：在加入战斗前对生成的实例调用 CardCmd.Upgrade（与 Largesse 生成升级无色牌同款）。
		if (upgraded)
		{
			CardCmd.Upgrade(card);
		}
		await CardPileCmd.AddGeneratedCardsToCombat([card], PileType.Hand, creator ?? owner);

		// 记录本场战斗为该玩家生成的术式数量 +1（供“造成生成数伤害”的卡牌使用）。
		await PowerCmd.Apply<ArtTrackerPower>(choiceContext, owner.Creature, 1m, owner.Creature, null);

		return card;
	}

	/// <summary>
	/// 判断一张牌是否为术式（术式-凋零/术式-枯萎/术式-破袭/术式-浸毒/术式-灾引）。
	/// </summary>
	public static bool IsArt(CardModel card)
	{
		return card is ArtOfDecay or ArtOfWilt or ArtOfBreach or ArtOfVenom or ArtOfDoom;
	}
}
