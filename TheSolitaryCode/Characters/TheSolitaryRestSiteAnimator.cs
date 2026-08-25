using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;

namespace TheSolitary.Characters;

/// <summary>
/// 火堆休息处动画播放器（作为休息场景的子节点，RitsuLib 转换场景时会被一起搬进 NRestSiteCharacter）。
/// 观者战斗骨骼（animations/characters/watcher/skeleton.skel）没有 overgrowth_loop / hive_loop / glory_loop
/// （那些是原版专属火堆骨骼的动画名），基类 NRestSiteCharacter 按幕数播放这三个动画会失败。
/// 这里等骨骼就绪后统一播放战斗骨骼自带的 relaxed_loop。
/// </summary>
public sealed partial class TheSolitaryRestSiteAnimator : Node
{
    public override void _Ready()
    {
        foreach (Node child in GetParent().GetChildren())
        {
            if (child is not Node2D node2D || node2D.GetClass() != "SpineSprite")
            {
                continue;
            }

            MegaSprite sprite = new(node2D);
            this.RunWhenSpineReady(sprite, animState =>
                animState.SetAnimation("relaxed_loop", loop: true));
        }
    }
}
