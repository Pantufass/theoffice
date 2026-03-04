using Godot;
using System;

public partial class Interactable : Node3D, IInteractable
{
    [Export] public string InteractText = "Interacted";
     public override void _Ready()
    {
        base._Ready();
    }


    public virtual void Interact(PlayerCharacter player)
    {
        player.SetText(InteractText);
    }

    public virtual void OnFocus(PlayerCharacter player)
    {
    }

    public virtual void OnUnfocus(PlayerCharacter player)
    {
        player.SetHint("");
    }

}
