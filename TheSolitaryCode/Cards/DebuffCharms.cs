using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Random;

namespace TheSolitary.Cards;

// 减益符：TheSolitary 的衍生牌集合（类似小刀），由其它卡牌/遗物生成后加入手牌。
// 共五种：缓慢、虚弱、易伤、浸毒、灾厄。
public static class DebuffCharms
{
	// 五种减益符的原型（canonical）实例，从 ModelDb 中获取。
	private static readonly CardModel[] All =
	[
		ModelDb.Card<SlowCharm>(),
		ModelDb.Card<WeakCharm>(),
		ModelDb.Card<VulnerableCharm>(),
		ModelDb.Card<PoisonCharm>(),
		ModelDb.Card<DoomCharm>()
	];

	/// <summary>
	/// 生成一张随机的减益符并加入手中（等价于 Shiv.CreateInHand 的模式）。
	/// 缓慢符只有在"至少一个可被攻击的敌人还没有缓慢效果"时才可能被选中，
	/// 避免在全员都已有缓慢时生成一张几乎无效的缓慢符。
	/// </summary>
	/// <param name="owner">持有者玩家。</param>
	/// <param name="combatState">当前战斗状态。</param>
	/// <param name="rng">用于随机挑选符的 RNG（建议传入 RunState.Rng.CombatCardGeneration）。</param>
	/// <param name="creator">生成来源（用于统计/来源记录，缺省为 owner）。</param>
	public static async Task<CardModel> CreateRandomInHand(Player owner, ICombatState combatState, Rng rng, Player? creator = null)
	{
		// 只要存在一个可命中敌人没有缓慢，就保留缓慢符作为候选；否则排除它。
		bool anyHittableEnemyWithoutSlow = combatState.HittableEnemies.Any(e => !e.HasPower<SlowPower>());
		IEnumerable<CardModel> candidates = anyHittableEnemyWithoutSlow
			? All
			: All.Where(c => c != ModelDb.Card<SlowCharm>());

		CardModel canonical = rng.NextItem(candidates)!;
		CardModel card = combatState.CreateCard(canonical, owner);
		await CardPileCmd.AddGeneratedCardsToCombat([card], PileType.Hand, creator ?? owner);
		return card;
	}
}
