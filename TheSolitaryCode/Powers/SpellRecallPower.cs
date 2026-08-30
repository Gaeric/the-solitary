using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using TheSolitary.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 术式回想的 Power（参考附魔造物 EnchantedCreationPower 的 AfterCardGeneratedForCombat 钩子）：
// 每当拥有者生成一张术式时，使其获得重放（Replay：打出时额外打一次，参考原版变形 Transfigure）。
[RegisterPower]
public sealed class SpellRecallPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	// 自定义图标（取材 WatcherBeautified 观者 Power 图标，128x128，小图与大图共用）。
	public override PowerAssetProfile AssetProfile => new(
		IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
		BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png");

	// 生成钩子：只处理拥有者自己生成的术式，为其增加重放，并刷新手牌中的卡面显示。
	public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
	{
		if (creator == null || creator.Creature != base.Owner || !Arts.IsArt(card))
		{
			return;
		}

		Flash();
		card.BaseReplayCount++;

		// 重放计数变化只触发手牌卡的闪烁（NHandCardHolder.ReplayCountChanged -> Flash），
		// 不会重绘卡面描述文本；而生成钩子在卡加入牌堆（首帧渲染）之后才执行，
		// 因此手牌中的术式卡面可能不会立即显示 "Replay ×2"。
		// 等一帧确保手牌 UI 已为该卡创建 holder 后，主动刷新卡面。
		// GetCard 只查手牌/选中/待打出容器；术式不在手牌（如抽牌堆）时返回 null，无需刷新。
		SceneTree sceneTree = (SceneTree)Engine.GetMainLoop();
		await sceneTree.ToSignal(sceneTree, SceneTree.SignalName.ProcessFrame);
		NPlayerHand.Instance?.GetCard(card)?.UpdateVisuals(card.Pile?.Type ?? PileType.Hand, CardPreviewMode.Normal);
	}
}
