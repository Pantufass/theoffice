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
    [Export] public string FoundText = "";
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
            player.SetText(FoundText);
            Active = false;
        }
        else base.Interact(player);
    }
    public override void OnFocus(PlayerCharacter player)
    {
        base.OnFocus(player);
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
