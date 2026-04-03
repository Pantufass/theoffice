using Godot;
using System;

public partial class HouseLevel : Node3D
{
    [Export] public Door doorKitchen;
    [Export] public Door doorArt;
    [Export] public Door doorBedroom;
    [Export] public TV TV;
    [Export] public PlayerCharacter player;

    public static Action NextStep;
    public static Action ItemFound;
    private int step = 0;
    private int stepItem = 0;
    public override void _Ready()
    {
        base._Ready();

        Node3D house = GetNode<Node3D>("House");
        Node3D doors = house.GetNode<Node3D>("Doors");
        if(doorArt == null) doorArt = doors.GetNode<Door>("DoorArt");
        if(doorBedroom == null) doorBedroom = doors.GetNode<Door>("DoorBedroom");
        if(doorKitchen == null) doorKitchen = doors.GetNode<Door>("DoorKitchen");

        if(player == null) player = GetNode<PlayerCharacter>("PlayerCharacter");
        if(TV == null) TV = house.GetNode<TV>("TV");

        NextStep += NextAction;
        ItemFound += ItemFoundAction;
    }

    public void NextAction()
    {
        if(stepItem != step) return;
        GD.Print("NEXT");
        if(step == 0) First();
        else if(step == 1) Second();
        else if(step == 2) Third();
        else if(step == 3) AllDone();
    }

    public void ItemFoundAction()
    {
        stepItem++;
        if(step == 1) doorArt.Active = true;
        else if(step == 2) doorBedroom.Active = true;
        else if(step == 3) doorKitchen.Active = true;
    }

    internal void SetItem(string itemName, KeyItem.EnumItemType itemType)
    {
        var name = "%"+ itemName;
        KeyItem item = GetNode<KeyItem>(name);
        if(item.ItemType == itemType)
        {
            item.Active = true;
        }
        else
        {
            throw new Exception("Wrong item assigned to " + itemName);
        }
    }

    internal void First()
    {
        SetItem("ArtItem", KeyItem.EnumItemType.Paiting);
        step = 1;
        TV.TurnOn();
        TV.UpdateImage(step);
    }
    internal void Second()
    {
        SetItem("Shirt",KeyItem.EnumItemType.Shirt);
        step = 2;
        TV.UpdateImage(step);
    }
    internal void Third()
    {
        SetItem("Bananas",KeyItem.EnumItemType.Bananas);
        step = 3;
        TV.UpdateImage(step);
    }

    internal void AllDone()
    {
        TV.UpdateImage(4);
    }

}
