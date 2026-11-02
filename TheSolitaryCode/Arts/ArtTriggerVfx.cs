using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.TestSupport;

namespace TheSolitary.ArtEffects;

internal static class ArtTriggerVfx
{
	private const string LightningScenePath = "res://scenes/vfx/vfx_attack_lightning.tscn";
	private const string RecolorShaderPath = $"{Entry.ResPath}/shaders/art_lightning.gdshader";
	private const string LightningSfx = "event:/sfx/characters/defect/defect_lightning_evoke";

	public static void Play(Creature target, ArtDefinition effect)
	{
		if (TestMode.IsOn || NonInteractiveMode.IsActive || target.IsDead)
		{
			return;
		}
		var creatureNode = target.GetCreatureNode();
		var container = target.GetVfxContainer();
		if (creatureNode == null || container == null)
		{
			return;
		}

		// 只复用原版闪电命中特效；它的动画会在约 0.33 秒后自行 queue_free。
		Node2D vfx = PreloadManager.Cache.GetScene(LightningScenePath).Instantiate<Node2D>();
		var material = new ShaderMaterial { Shader = GD.Load<Shader>(RecolorShaderPath) };
		material.SetShaderParameter("tint", effect.Color);
		vfx.GetNode<Sprite2D>("Animation").Material = material;
		vfx.Scale *= effect.VfxScale;
		container.AddChildSafely(vfx);
		vfx.GlobalPosition = creatureNode.GlobalPosition;
		SfxCmd.Play(LightningSfx);
	}
}