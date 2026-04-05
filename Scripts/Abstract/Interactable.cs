using Godot;
using System.Collections.Generic;

public partial class Interactable : Node3D, IInteractable
{
	[Export] public bool DebugMode = false;
	[Export] public bool GlowOn = true;
	[Export] public string InteractText = "Interacted";
	[Export] public Color GlowColor = new Color(1, 1, 0.7f, 0.03f);
	[Export] public float GlowWidth = 0.002f;
	private string shaderPath = "res://UI/Shaders/edge_glow.gdshader";
	
	private List<MeshInstance3D> _originalMeshes = new List<MeshInstance3D>();
	private List<MeshInstance3D> _glowMeshes = new List<MeshInstance3D>();
	private ShaderMaterial _glowMaterial;

	public override void _Ready()
	{
		base._Ready();
		
		// Create glow material
		_glowMaterial = new ShaderMaterial();
		_glowMaterial.Shader = GD.Load<Shader>(shaderPath);
		_glowMaterial.SetShaderParameter("glow_color", GlowColor);
		_glowMaterial.SetShaderParameter("glow_width", GlowWidth);
		
		// Find all meshes
		FindAllMeshes(this);
		
		// Create glow copies
		foreach (var mesh in _originalMeshes)
		{
			CreateGlowMesh(mesh);
		}
		var tempGlow = GlowOn;
		GlowOn = true;
		// Start hidden
		ShowGlow(false);
		GlowOn = tempGlow;
	}
	
	private void FindAllMeshes(Node node)
	{
		foreach (var child in node.GetChildren())
		{
			if (child is MeshInstance3D mesh)
			{
				_originalMeshes.Add(mesh);
			}
			FindAllMeshes(child);
		}
	}
	
	private void CreateGlowMesh(MeshInstance3D originalMesh)
	{
		if (originalMesh.Mesh == null) return;
		
		var glow = new MeshInstance3D();
		glow.Name = $"{originalMesh.Name}_Glow";
		glow.Mesh = originalMesh.Mesh;
		glow.MaterialOverride = _glowMaterial;
		glow.Layers = originalMesh.Layers;
		
		// FIX: Copy the EXACT transform including position, rotation, and scale
		glow.Transform = originalMesh.Transform; // This copies everything
		
		// Add as child of the PARENT so it stays at the same level
		// This ensures transforms are independent but start matched
		originalMesh.GetParent().AddChild(glow);
		
		_glowMeshes.Add(glow);
		
		GD.Print($"Created glow for {originalMesh.Name} at position {originalMesh.Position}");
	}
	
	private void ShowGlow(bool show)
	{
		if(!GlowOn) return;
		foreach (var glow in _glowMeshes)
		{
			if (glow != null)
				glow.Visible = show;
		}
	}
	
	// Keep them in sync every frame (in case something moves)
	public override void _Process(double delta)
	{
		if (!Visible) return;
		
		for (int i = 0; i < _originalMeshes.Count && i < _glowMeshes.Count; i++)
		{
			if (_glowMeshes[i].Visible)
			{
				// Update to match original mesh's global transform
				_glowMeshes[i].GlobalTransform = _originalMeshes[i].GlobalTransform;
			}
		}
	}
	
	public virtual void OnFocus(PlayerCharacter player)
	{
		ShowGlow(true);
		if(DebugMode)
			player.SetHint("FOCUSED");
	}

	public virtual void OnUnfocus(PlayerCharacter player)
	{
		ShowGlow(false);
		if(DebugMode)
			player.SetHint("");
	}
	
	public virtual void Interact(PlayerCharacter player)
	{
		player.SetText(InteractText);
	}
}
