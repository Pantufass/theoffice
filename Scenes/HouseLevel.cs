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
        if(doorArt == null) doorArt = doors.GetNode<Node3D>("DoorArt").GetNode<Door>("DoorMesh");
        if(doorBedroom == null) doorBedroom = doors.GetNode<Node3D>("DoorBedroom").GetNode<Door>("DoorMesh");
        if(doorKitchen == null) doorKitchen = doors.GetNode<Node3D>("DoorKitchen").GetNode<Door>("DoorMesh");

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
        TV.UpdateTVText("Drawing is for \n weak men"); 
        SetItem("ArtItem", KeyItem.EnumItemType.Paiting);
        step = 1;
    }
    internal void Second()
    {
        TV.UpdateTVText("Don't dress up or \n you are gay");
        SetItem("Shirt",KeyItem.EnumItemType.Shirt);
        step = 2;
    }
    internal void Third()
    {
        TV.UpdateTVText("Eating bananas is gay");
        SetItem("Bananas",KeyItem.EnumItemType.Bananas);
        step = 3;
    }

    internal void AllDone()
    {
        TV.UpdateTVText("Real men only surround \n themselves with hard men");
        player.SetText("uf im not gay");
    }

}
