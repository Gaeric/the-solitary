using System.Globalization;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace TheSolitary.ArtEffects;

public static class ArtHoverTips
{
	private const string LocPrefix = "THE_SOLITARY_ART_HOVER";

	// null 用于只监听术式的来源：同时展示普通 / 升级数值，不依赖来源自身是否升级。
	public static IEnumerable<IHoverTip> Create(bool? upgraded = null)
	{
		yield return new HoverTip(Text("title"), Text("description"));

		List<string> lines = [];
		foreach (ArtKind kind in Enum.GetValues<ArtKind>())
		{
			ArtDefinition normal = ArtDefinition.Get(kind);
			ArtDefinition enhanced = ArtDefinition.Get(kind, upgraded: true);
			(string cardKey, string powerKey) = kind switch
			{
				ArtKind.Decay => ("ART_OF_DECAY", "SLOW_POWER"),
				ArtKind.Wilt => ("ART_OF_WILT", "WEAK_POWER"),
				ArtKind.Breach => ("ART_OF_BREACH", "VULNERABLE_POWER"),
				ArtKind.Venom => ("ART_OF_VENOM", "POISON_POWER"),
				ArtKind.Doom => ("ART_OF_DOOM", "DOOM_POWER"),
				_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "未知的术式类型。")
			};
			LocString line = Text("referenceLine");
			line.Add("Name", new LocString("cards", $"THE_SOLITARY_CARD_{cardKey}.title"));
			line.Add("Damage", FormatValue(normal.Damage, enhanced.Damage, upgraded));
			line.Add("PowerAmount", FormatValue(normal.PowerAmount, enhanced.PowerAmount, upgraded));
			line.Add("Effect", new LocString("powers", $"{powerKey}.title"));
			lines.Add(line.GetFormattedText());
		}
		if (upgraded == null)
		{
			lines.Add(Text("comparisonNote").GetFormattedText());
		}
		string titleKey = upgraded switch
		{
			true => "upgradedReferenceTitle",
			false => "referenceTitle",
			null => "comparisonReferenceTitle"
		};
		yield return new HoverTip(Text(titleKey), string.Join("\n", lines));
	}

	private static LocString Text(string key) => new("cards", $"{LocPrefix}.{key}");

	private static string FormatValue(decimal normal, decimal enhanced, bool? upgraded)
	{
		if (upgraded == null && normal != enhanced)
		{
			return $"{normal.ToString(CultureInfo.InvariantCulture)}/{enhanced.ToString(CultureInfo.InvariantCulture)}";
		}
		return (upgraded == true ? enhanced : normal).ToString(CultureInfo.InvariantCulture);
	}
}