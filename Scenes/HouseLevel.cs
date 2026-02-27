using Godot;
using System;

public partial class HouseLevel : Node3D
{
    [Export] public Door doorKitchen;
    [Export] public Door doorArt;
    [Export] public Door doorBedroom;
    [Export] public TV TV;
    [Export] public PlayerCharacter player;

    public static Action Next;
    private int step = 0;
    public override void _Ready()
    {
        base._Ready();

        Node3D house = GetNode<Node3D>("House");
        if(doorArt == null) doorArt = house.GetNode<Node3D>("DoorArt").GetNode<Door>("DoorMesh");
        if(doorBedroom == null) doorBedroom = house.GetNode<Node3D>("DoorBedroom").GetNode<Door>("DoorMesh");
        if(doorKitchen == null) doorKitchen = house.GetNode<Node3D>("DoorKitchen").GetNode<Door>("DoorMesh");

        if(player == null) player = GetNode<PlayerCharacter>("PlayerCharacter");
        if(TV == null) TV = house.GetNode<TV>("TV");

        Next += NextAction;
    }

    public void NextAction()
    {
        GD.Print("NEXT");
        if(step == 0) First();
        else if(step == 1) Second();
        else if(step == 2) Third();
        else if(step == 3) AllDone();
    }

    internal void First()
    {
        TV.UpdateTVText("Drawing is for \n weak men");
        doorArt.Active = true;
        step = 1;
    }
    internal void Second()
    {
        TV.UpdateTVText("Don't dress up or \n you are gay");
        doorBedroom.Active = true;
        step = 2;
    }
    internal void Third()
    {
        TV.UpdateTVText("Eating bananas is gay");
        doorKitchen.Active = true;
        step = 3;
    }

    internal void AllDone()
    {
        TV.UpdateTVText("Real men only surround \n themselves with hard men");
        player.SetText("uf im not gay");
    }

}
