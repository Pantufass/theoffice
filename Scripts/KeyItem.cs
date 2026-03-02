using Godot;
using System;

public partial class KeyItem : Interactable
{
    public enum EnumItemType
    {
        Bananas,
        Paiting,
        Shirt,
        Other
    }
    [Export]public EnumItemType ItemType = EnumItemType.Other;
    private bool _active = false;
    public bool Active{get => _active; set => _active = value;}
    public override void _Ready()
    {
        base._Ready();
    }
    public override void Interact(PlayerCharacter player)
    {
        if(Active)
        {
            HouseLevel.ItemFound?.Invoke();
        }
        base.Interact(player);
    }
    public override void OnFocus(PlayerCharacter player)
    {
        if(Active)
        {
            player.SetHint("Interact to know more");
        }
        else
        {
            player.SetHint("");
        }
    }
}
