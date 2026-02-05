using Godot;
using System;

public partial class OnPc : CanvasLayer
{

    [Export] public ColorRect Typing;
    [Export] public ColorRect CharSelect;
    [Export] public Label Read;
    [Export] public Label Write;
    public override void _Ready()
    {
        if(Typing == null) Typing = GetNode<ColorRect>("Typing");
        if(CharSelect == null) CharSelect = GetNode<ColorRect>("CharSelect");
        if(Read == null) Read = Typing.GetNode<Label>("Read");
        if(Write == null) Write = Typing.GetNode<Label>("Write");
        Read.Text = "Things to copy";
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
