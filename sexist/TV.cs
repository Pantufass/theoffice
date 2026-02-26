using Godot;
using System;

public partial class TV : Node3D, IInteractable
{
    [Export] public MeshInstance3D Screen;
    [Export] public Viewport SubViewport;

    public override void _Ready()
    {
        base._Ready();
        if(Screen == null) Screen = GetNode<MeshInstance3D>("Screen");
        if(SubViewport == null) SubViewport = Screen.GetNode<SubViewport>("SubViewport");

        StandardMaterial3D screenMaterial = new StandardMaterial3D();
        screenMaterial.AlbedoTexture = SubViewport.GetTexture();
        Screen.MaterialOverride = screenMaterial;
    }

    public void Interact(PlayerCharacter player)
    {
        player.ZoomToTV(this.Transform);
    }

    public void OnFocus()
    {
    }

    public void OnUnfocus()
    {
    }

}
