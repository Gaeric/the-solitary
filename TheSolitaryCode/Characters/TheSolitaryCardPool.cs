using Godot;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace TheSolitary.Characters;

public sealed class TheSolitaryCardPool : TypeListCardPoolModel
{
    // 卡框材质：使用游戏自带 hsv.gdshader，参数与原版观者紫色卡框 card_frame_purple_mat.tres 完全一致
    // （h=0.715 → 257° 紫色）。不要用 CreateReplaceHueShaderMaterial——它的参数是目标 RGB（0.42,0.65,0.72
    // 实为青蓝色 #6BA6B8），且按源纹理饱和度向灰度稀释，染不出原版卡框那种紫色。
    private static readonly Material? PoolFrameTintMaterial =
        MaterialUtils.CreateHsvShaderMaterial(0.715f, 0.65f, 1.1f);

    // Title 和 EnergyColorName 是池子的稳定标识，不是玩家看到的角色名。
    // 自定义角色卡、遗物、药水池保持同一个 EnergyColorName，方便实验室和文本统一读取能量图标。
    public override string Title => "TheSolitary";
    public override string EnergyColorName => "TheSolitary";

    // 这里指定卡牌文本和大图使用的能量图标路径。
    // res://TheSolitary/... 里的 TheSolitary 是 PCK 资源目录，不是 C# namespace。
    public override string? BigEnergyIconPath => $"{Entry.ResPath}/images/characters/energy_big.png";
    public override string? TextEnergyIconPath => $"{Entry.ResPath}/images/characters/energy_text.png";

    public override Color DeckEntryCardColor => TheSolitaryCharacter.ThemeColor;
    public override Color EnergyOutlineColor => new(0.08f, 0.18f, 0.24f);
    public override Material? PoolFrameMaterial => PoolFrameTintMaterial;

    // false 表示这是角色专属卡池，不是事件/状态那类无色卡池。
    public override bool IsColorless => false;
}
