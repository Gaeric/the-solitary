using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Relics;

// 元能核心（稀有遗物）：每打出 10 张附魔牌，获得 1 点能量。
// 计数跨战斗累计（"每打出 N 张"语义，与元能吸附 EnergyAbsorptionPower 同款，仅将阈值改为 10）。
[RegisterRelic(typeof(TheSolitaryRelicPool))]
public sealed class ArcaneCore : ModRelicTemplate
{
    // 触发阈值：每打出几张附魔牌结算一次。
    private const int EnchantedCardsThreshold = 10;

    // 自上次获得能量以来打出的附魔牌数量（跨战斗累计，用模运算判定触发）。
    private int _enchantedCardsPlayed;

    public override RelicRarity Rarity => RelicRarity.Rare;

    // 遗物角标显示当前进度（距离下一次结算的附魔牌数）。
    public override bool ShowCounter => true;

    public override int DisplayAmount
    {
        get
        {
            int threshold = DynamicVars.Cards.IntValue;
            return EnchantedCardsPlayed % threshold;
        }
    }

    // 基础数值：阈值 + 能量（{Cards:diff()} / {Energy:energyIcons()} 占位符）。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(EnchantedCardsThreshold),
        new EnergyVar(1)
    ];

    private int EnchantedCardsPlayed
    {
        get => _enchantedCardsPlayed;
        set
        {
            AssertMutable();
            _enchantedCardsPlayed = value;
            UpdateDisplay();
        }
    }

    // 图片资源统一放在 AssetProfile 里配置（三个路径先指向同一张图）。
    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png");

    private void UpdateDisplay()
    {
        int threshold = DynamicVars.Cards.IntValue;
        base.Status = (EnchantedCardsPlayed % threshold == threshold - 1) ? RelicStatus.Active : RelicStatus.Normal;
        InvokeDisplayAmountChanged();
    }

    // 打出附魔牌时累计；达到阈值后获得能量。
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner || cardPlay.Card.Enchantment == null)
        {
            return;
        }

        EnchantedCardsPlayed++;
        int threshold = DynamicVars.Cards.IntValue;
        if (EnchantedCardsPlayed % threshold != 0)
        {
            return;
        }

        Flash();
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, base.Owner);
    }
}
