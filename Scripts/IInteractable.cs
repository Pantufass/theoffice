using Godot;
using System;

public interface IInteractable
{
    void Interact(PlayerCharacter player);
    void OnFocus();
    void OnUnfocus();

}
