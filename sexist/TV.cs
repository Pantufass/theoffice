using Godot;
using System;

public partial class TV : Node3D, IInteractable
{
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
