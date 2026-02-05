using Godot;
using System;

public partial class OnPc : CanvasLayer
{
    [Export] public Label Read;
    [Export] public Label Write;
    public override void _Ready()
    {
        if(Read == null) Read = GetNode<Label>("Read");
        if(Write == null) Write = GetNode<Label>("Write");
        Read.Text = "Thongs to copy";
    }

    public override void _Process(double delta)
    {
        
    }

    public override void _Input(InputEvent e)
    {
        if (e is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Unicode != 0)
    {
        char c = (char)keyEvent.Unicode;
        GD.Print(c);
    }
    }
}
