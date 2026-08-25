using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;

namespace TheSolitary.Characters;

/// <summary>
/// 选人界面背景 Spine 自动播放器。
/// 观者的选人骨骼（animations/character_select/watcher/characterselect_watcher.skel）只有 1 个动画（animation），
/// 等父节点 SpineSprite 的骨骼就绪后自动播放该动画。功能与原版 NSpineAutoPlayer 相同，
/// 但脚本位于本 Mod 内，避免 tscn 直接引用游戏脚本导致 pck 导出失败。
/// </summary>
public sealed partial class TheSolitaryCharSelectAutoPlayer : Node
{
    public override void _Ready()
    {
        MegaSprite sprite = new(GetParent());
        this.RunWhenSpineReady(sprite, animState =>
        {
            MegaSkeleton? skeleton = sprite.GetSkeleton();
            if (skeleton == null)
            {
                return;
            }
            IReadOnlyList<string> animationNames = skeleton.GetData().GetAnimationNames();
            if (animationNames.Count == 1)
            {
                animState.SetAnimation(animationNames[0]);
            }
        });
    }
}
