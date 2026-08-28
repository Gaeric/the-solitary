using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Godot;

namespace TheSolitary.Characters;

[RegisterCharacter]
public sealed class TheSolitaryCharacter : ModCharacterTemplate<TheSolitaryCardPool, TheSolitaryRelicPool, TheSolitaryPotionPool>
{
    // 主题色（紫色 #8B5CF6）：色相约 258°，与卡框的观者紫色（h=0.715→257°）同色系。
    // 用于角色名/地图标记/牌组条目/药水与遗物实验室描边。
    public static readonly Color ThemeColor = new(0.545f, 0.361f, 0.965f);

    private const string SceneRoot = $"{Entry.ResPath}/scenes/characters";
    private const string ImageRoot = $"{Entry.ResPath}/images/characters";
    private const string CharacterScenePath = $"{SceneRoot}/TheSolitary_character.tscn";
    private const string EnergyCounterScenePath = $"{SceneRoot}/TheSolitary_energy_counter.tscn";
    private const string MerchantScenePath = $"{SceneRoot}/TheSolitary_merchant.tscn";
    private const string RestSiteScenePath = $"{SceneRoot}/TheSolitary_rest_site.tscn";
    private const string CharacterSelectBgScenePath = $"{SceneRoot}/TheSolitary_character_select_bg.tscn";

    // 角色名称颜色。
    public override Color NameColor => ThemeColor;
    // 能量图标轮廓颜色。
    public override Color EnergyLabelOutlineColor => new(0.08f, 0.18f, 0.24f);
    // 地图绘制颜色。
    public override Color MapDrawingColor => ThemeColor;

    // 人物性别（男女中立）。
    public override CharacterGender Gender => CharacterGender.Neutral;

    // 初始血量和金币。
    public override int StartingHp => 75;
    public override int StartingGold => 99;

    // CharacterAssetProfile 按类别拆分。你只写需要替换的部分，其他字段会保留回退。
    // AssetProfile 只指定模板自带的静态占位资源；没有复制的音频、拖尾、转场等资源继续从占位角色回退。
    public override CharacterAssetProfile AssetProfile => new(
        Scenes: new CharacterSceneAssetSet(
            // 人物模型 tscn 路径。
            VisualsPath: CharacterScenePath,
            // 能量表盘 tscn 路径。
            EnergyCounterPath: EnergyCounterScenePath,
            // 商店人物场景。
            MerchantAnimPath: MerchantScenePath,
            // 篝火休息场景。
            RestSiteAnimPath: RestSiteScenePath),
        Ui: new CharacterUiAssetSet(
            // 人物头像路径。
            IconTexturePath: $"{ImageRoot}/TheSolitary_character_icon.png",
            // 人物头像轮廓。
            IconOutlineTexturePath: $"{ImageRoot}/TheSolitary_character_icon_outline.png",
            // 游戏左上角头像、角色统计页头像、每日挑战角色头像。是场景不是图片。
            IconPath: $"{SceneRoot}/TheSolitary_icon.tscn",
            // 人物选择背景。
            CharacterSelectBgPath: CharacterSelectBgScenePath,
            // 人物选择图标。
            CharacterSelectIconPath: $"{ImageRoot}/TheSolitary_character_select.png",
            // 人物选择图标-锁定状态。
            CharacterSelectLockedIconPath: $"{ImageRoot}/TheSolitary_character_select_locked.png",
            // 地图上的角色标记图标、表情轮盘上的角色头像。
            MapMarkerPath: $"{ImageRoot}/TheSolitary_map_marker.png"));

    // 某个字段没写时，RitsuLib 会从占位角色配置里补齐。
    public override string? PlaceholderCharacterId => "ironclad";
    // 如果你的人物不需要时间线小故事，加上这句。
    public override bool RequiresEpochAndTimeline => false;
    // 攻击和施法动画延迟，以对齐动画。静态占位资源不需要延迟。
    public override float AttackAnimDelay => 0f;
    public override float CastAnimDelay => 0f;

    // 让 RitsuLib 把普通 Godot 场景转换成游戏需要的 NCreatureVisuals。
    // 自动转换人物场景，让你不需要手动挂脚本。复制即可。
    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(
            CharacterScenePath);
    }

    // 观者骨骼动画名与游戏标准名不同（Idle/Attack/Cast/Hit/Dead/relaxed），
    // 通过 CreatureAnimator 把标准状态触发器映射到观者的动画名。
    protected override CreatureAnimator? SetupCustomCreatureAnimator(MegaSprite controller)
    {
        AnimState idle = new("Idle", isLooping: true);
        AnimState cast = new("Cast");
        AnimState attack = new("Attack");
        AnimState hit = new("Hit");
        AnimState dead = new("Dead");
        AnimState relaxed = new("relaxed", isLooping: true);

        cast.NextState = idle;
        attack.NextState = idle;
        hit.NextState = idle;

        CreatureAnimator animator = new(idle, controller);
        animator.AddAnyState(CreatureAnimator.idleTrigger, idle);
        animator.AddAnyState(CreatureAnimator.deathTrigger, dead);
        animator.AddAnyState(CreatureAnimator.hitTrigger, hit);
        animator.AddAnyState(CreatureAnimator.attackTrigger, attack);
        animator.AddAnyState(CreatureAnimator.castTrigger, cast);
        animator.AddAnyState("Relaxed", relaxed);
        return animator;
    }

    // 攻击建筑师的攻击特效列表。
    public override List<string> GetArchitectAttackVfx()
    {
        return
        [
            "vfx/vfx_attack_blunt",
            "vfx/vfx_heavy_blunt",
            "vfx/vfx_attack_slash",
            "vfx/vfx_bloody_impact",
            "vfx/vfx_rock_shatter"
        ];
    }
}
