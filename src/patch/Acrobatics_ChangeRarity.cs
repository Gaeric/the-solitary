using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace TheSolitary.Scripts;
[HarmonyPatch(typeof(Acrobatics), MethodType.Constructor)]
public static class AcrobaticsChangeRarity
{
    // Statically cached field; reflection is performed only once on first load
    private static readonly FieldInfo RarityField = 
        typeof(CardModel).GetField("<Rarity>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static void Postfix(Acrobatics __instance) // Harmony convention recommends naming the instance parameter __instance
    {
        if (RarityField != null)
        {
            // Set the rarity to enum value 2
            RarityField.SetValue(__instance, (CardRarity)2);
        }
    }
}