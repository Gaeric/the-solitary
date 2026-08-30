using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Relics;

// 委靡之阵（稀有遗物）：在每场战斗开始时，所有敌人失去 1 点力量、1 点敏捷。
// 参考原版红面具 RedMask（开局给所有敌人施加虚弱）的 BeforeSideTurnStart + TurnNumber<=1 模式：
// 敌方回合开始时 participants 不含我方，天然只在玩家侧第一回合前执行一次。
[RegisterRelic(typeof(TheSolitaryRelicPool))]
public sealed class SappingFormation : ModRelicTemplate
{
    // 力量 / 敏捷流失数值。
    private const decimal StatLoss = 1m;

    public override RelicRarity Rarity => RelicRarity.Rare;

    // 基础数值：力量流失 / 敏捷流失（{StrengthPower:diff()} / {DexterityPower:diff()} 占位符，参考焦散 Caustic）。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<StrengthPower>(StatLoss),
        new PowerVar<DexterityPower>(StatLoss)
    ];

    // 图片资源统一放在 AssetProfile 里配置（三个路径先指向同一张图）。
    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png");

    // 战斗第一回合开始前：所有敌人失去力量与敏捷（负值施加，参考萎靡 Malaise）。
    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(base.Owner.Creature))
        {
            return;
        }
        if (base.Owner.PlayerCombatState!.TurnNumber > 1)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<StrengthPower>(
            choiceContext, combatState.HittableEnemies, -DynamicVars.Strength.BaseValue, base.Owner.Creature, null);
        await PowerCmd.Apply<DexterityPower>(
            choiceContext, combatState.HittableEnemies, -DynamicVars.Dexterity.BaseValue, base.Owner.Creature, null);
    }
}
