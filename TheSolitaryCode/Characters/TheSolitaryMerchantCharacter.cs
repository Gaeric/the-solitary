using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace TheSolitary.Characters;

/// <summary>
/// 商店人物场景根节点：继承 NMerchantCharacter，使 RitsuLib 把本场景当作商人节点直接使用，
/// 避免把普通 Node2D 根包一层导致「双重缩放」且 GetChild(0) 取不到 SpineSprite。
/// 这里直接对 SpineSprite 子节点播放观者骨骼自带的 relaxed_loop。
/// </summary>
public sealed partial class TheSolitaryMerchantCharacter : NMerchantCharacter
{
    public override void _Ready()
    {
        foreach (Node child in GetChildren())
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
