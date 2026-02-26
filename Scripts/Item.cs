using Godot;
using System;

public partial class Item : RigidBody3D, IPickupable
{
    public enum EnumItemType
    {
        Tape,
        Gear,
        Crowbar,
        Belt,
        Other
    }
    [Export]public EnumItemType ItemType = EnumItemType.Other;

    [Export] public Vector3 RotationOffset = Vector3.Zero;

     public override void _Ready()
    {
        base._Ready();
    }


    public void Interact(PlayerCharacter player)
    {
        player.SetCurrentPickupable?.Invoke(this);
    }

    public virtual void Use()
    {
    }   
    public void OnFocus()
    {
        GD.Print("Focus on Item of type " + ItemType);
    }

    public void OnUnfocus()
    {
    }
    public void OnPickup()
    {
    
        if (this is RigidBody3D body)
        {
            body.Freeze = true;                    
            body.LinearVelocity = Vector3.Zero;
            body.AngularVelocity = Vector3.Zero;
            body.CollisionLayer = 0;
            body.CollisionMask = 0;
            body.Rotation = RotationOffset;
        }
        MeshInstance3D mesh = GetNodeOrNull<MeshInstance3D>("Mesh");
        if(mesh != null)
            mesh.Layers = 2;
        else throw new Exception("PLEASE");
    }

    private void SetLayerRecursive(Node node, uint layer)
    {
        if (node is MeshInstance3D mesh)
        {
            mesh.Layers = layer;
            GD.Print("Set layer of " + mesh.Name + " to " + layer +"INSHALLAH");
        }

        foreach (Node child in node.GetChildren())
            SetLayerRecursive(child, layer);
    }

    private void SetRenderLayer(Node node, uint layer = 1)
{
    if (node is VisualInstance3D visual)
    {
        visual.Layers = layer;
    }

    foreach (Node child in node.GetChildren())
    {
        SetRenderLayer(child, layer);
    }
}
    
    public void OnDrop()
    {
        Reparent(GetTree().CurrentScene);
        Freeze = false;

        CollisionLayer = 1;
        CollisionMask = 1;
        
        MeshInstance3D mesh = GetNodeOrNull<MeshInstance3D>("Mesh");
        if(mesh != null)
            mesh.Layers = 1;
    }
}
