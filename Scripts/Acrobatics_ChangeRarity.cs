using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace TheSolitary.Scripts;
[HarmonyPatch(typeof(Acrobatics), MethodType.Constructor)]
public static class AcrobaticsChangeRarity
{
    // 静态缓存字段，只在第一次加载时反射一次
    private static readonly FieldInfo RarityField = 
        typeof(CardModel).GetField("<Rarity>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static void Postfix(Acrobatics __instance) // Harmony 规范中建议实例使用 __instance 命名
    {
        if (RarityField != null)
        {
            // 将稀有度修改为枚举值 2
            RarityField.SetValue(__instance, (CardRarity)2);
        }
    }
}