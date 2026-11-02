using Godot;

namespace TheSolitary.ArtEffects;

// 数值与表现共用一份定义；术式+不再依赖 CardCmd.Upgrade。
public readonly record struct ArtDefinition(decimal Damage, decimal PowerAmount, Color Color, float VfxScale)
{
	public const float NormalVfxScale = 1f;
	public const float UpgradedVfxScale = 1.35f;

	public static ArtDefinition Get(ArtKind kind, bool upgraded = false)
	{
		(decimal damage, decimal powerAmount, string color) = kind switch
		{
			ArtKind.Decay => (upgraded ? 3m : 2m, 1m, "65D9FF"),
			ArtKind.Wilt => (upgraded ? 3m : 2m, upgraded ? 2m : 1m, "B8DF60"),
			ArtKind.Breach => (upgraded ? 3m : 2m, upgraded ? 2m : 1m, "FF9860"),
			ArtKind.Venom => (upgraded ? 2m : 1m, upgraded ? 3m : 2m, "45EE96"),
			ArtKind.Doom => (upgraded ? 3m : 2m, upgraded ? 5m : 3m, "CC7DFF"),
			_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "未知的术式类型。")
		};

		return new ArtDefinition(damage, powerAmount, new Color(color),
			upgraded ? UpgradedVfxScale : NormalVfxScale);
	}
}