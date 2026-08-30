using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TheSolitary.Cards;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Relics;

// 四象之阵（商店遗物）：在每场战斗中，你打出四种不同的术式后，所有敌人失去 2 点力量与 2 点敏捷。
// 五种术式各占一个 bit 位，用位掩码记录本场战斗已打出过哪些术式（可序列化，避免保存 HashSet）。
// 每场战斗开始时清零，同一场战斗只触发一次。
[RegisterRelic(typeof(TheSolitaryRelicPool))]
public sealed class FourSymbolsFormation : ModRelicTemplate
{
    // 需要打出的不同术式种类数。
    private const int DistinctArtsRequired = 4;

    // 力量 / 敏捷流失数值。
    private const decimal StatLoss = 2m;

    // 本场战斗已打出的不同术式位掩码（5 种术式各占 1 bit）。
    private int _artsMask;

    // 本场战斗是否已经触发过全体削弱。
    private bool _debuffTriggered;

    public override RelicRarity Rarity => RelicRarity.Shop;

    // 遗物角标显示本场战斗已打出的不同术式数（0~4）。
    public override bool ShowCounter => true;

    public override int DisplayAmount => CountBits(DistinctArtsMask);

    // 基础数值：力量流失 / 敏捷流失（{StrengthPower:diff()} / {DexterityPower:diff()} 占位符，参考焦散 Caustic）。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<StrengthPower>(StatLoss),
        new PowerVar<DexterityPower>(StatLoss)
    ];

    private int DistinctArtsMask
    {
        get => _artsMask;
        set
        {
            AssertMutable();
            _artsMask = value;
            UpdateDisplay();
        }
    }

    private bool DebuffTriggered
    {
        get => _debuffTriggered;
        set
        {
            AssertMutable();
            _debuffTriggered = value;
        }
    }

    // 图片资源统一放在 AssetProfile 里配置（三个路径先指向同一张图）。
    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png");

    private void UpdateDisplay()
    {
        base.Status = (CountBits(DistinctArtsMask) >= DistinctArtsRequired) ? RelicStatus.Active : RelicStatus.Normal;
        InvokeDisplayAmountChanged();
    }

    // 每场战斗开始时清零计数（同一场战斗只触发一次）。
    public override Task BeforeCombatStart()
    {
        DistinctArtsMask = 0;
        DebuffTriggered = false;
        return Task.CompletedTask;
    }

    // 打出术式时记录种类；集齐四种不同术式后对所有敌人施加力量/敏捷流失。
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner || !Arts.IsArt(cardPlay.Card) || DebuffTriggered)
        {
            return;
        }

        int bit = ArtBit(cardPlay.Card);
        if (bit == 0)
        {
            return;
        }

        DistinctArtsMask |= bit;
        if (CountBits(DistinctArtsMask) < DistinctArtsRequired)
        {
            return;
        }

        DebuffTriggered = true;
        Flash();
        foreach (Creature enemy in base.Owner.Creature.CombatState!.HittableEnemies)
        {
            // 负值施加，参考萎靡 Malaise / 焦散 Caustic。
            await PowerCmd.Apply<StrengthPower>(choiceContext, enemy, -DynamicVars.Strength.BaseValue, base.Owner.Creature, null);
            await PowerCmd.Apply<DexterityPower>(choiceContext, enemy, -DynamicVars.Dexterity.BaseValue, base.Owner.Creature, null);
        }
    }

    // 把术式牌映射到位掩码中的 1 bit（升级版仍是同类，类型判断不变）。
    private static int ArtBit(CardModel card) => card switch
    {
        ArtOfDecay => 1 << 0,
        ArtOfWilt => 1 << 1,
        ArtOfBreach => 1 << 2,
        ArtOfVenom => 1 << 3,
        ArtOfDoom => 1 << 4,
        _ => 0
    };

    // 统计位掩码中置位的数量（Kernighan 算法）。
    private static int CountBits(int mask)
    {
        int count = 0;
        while (mask != 0)
        {
            mask &= mask - 1;
            count++;
        }
        return count;
    }
}
