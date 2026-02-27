using Godot;
using System;

public interface IInteractable
{
    void Interact(PlayerCharacter player);
    void OnFocus(PlayerCharacter player);
    void OnUnfocus(PlayerCharacter player);

}
