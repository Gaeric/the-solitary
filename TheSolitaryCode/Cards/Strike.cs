using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 初始打击牌。RegisterCard 会把这张牌交给 RitsuLib 自动注册；
// RegisterCharacterStarterCard 会把它追加进 TheSolitaryCharacter 的初始卡组（4 张）。
[RegisterCard(typeof(TheSolitaryCardPool))]
[RegisterCharacterStarterCard(typeof(TheSolitaryCharacter), 4)]
public sealed class Strike : ModCardTemplate
{
    // 基础耗能。
    private const int BaseEnergyCost = 1;
    // 卡牌类型。
    private const CardType CardKind = CardType.Attack;
    // 卡牌稀有度。
    private const CardRarity CardRarityValue = CardRarity.Basic;
    // 目标类型（AnyEnemy 表示任意敌人）。
    private const TargetType CardTarget = TargetType.AnyEnemy;
    // 是否在卡牌图鉴中显示。
    private const bool ShowInCardLibrary = true;

    // 卡图资源；文件名与类名一致（TheSolitary/images/cards/Strike.png）。
    // 这里的 res://TheSolitary/... 是 Godot 资源路径，对应的是你的资源文件夹名字。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    // CanonicalVars 翻译是“规范值”，指卡牌的基础数值。
    // 添加一个 DamageVar 意为指定卡牌的基础伤害是多少；它会自动绑定到本地化里的 {Damage:diff()} 占位符。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6, ValueProp.Move)
    ];

    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Strike };

    public Strike() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑。
    // 尖塔2使用了 async 和 await 来控制效果逻辑顺序执行，和尖塔1的 action 类似。
    // DamageCmd.Attack 会按当前 DynamicVars.Damage 的值造成攻击伤害。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
    }

    // 升级后的效果逻辑。
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
    }
}
