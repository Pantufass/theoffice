using Godot;
using System;

public partial class TV : Node3D, IInteractable
{
    [Export] public MeshInstance3D Screen;
    [Export] public SubViewport SubViewport;

    public override void _Ready()
    {
        base._Ready();
        if(Screen == null) Screen = GetNode<MeshInstance3D>("Screen");
        if(SubViewport == null) SubViewport = Screen.GetNode<SubViewport>("SubViewport");

        StandardMaterial3D screenMaterial = new StandardMaterial3D();
        screenMaterial.AlbedoTexture = SubViewport.GetTexture();
        Screen.MaterialOverride = screenMaterial;

        SetupTVText("Booting up");
    }

    public void Interact(PlayerCharacter player)
    {
        if (!player.Sitting)
        {
            player.ZoomToTV(GlobalTransform);
        }
        else
        {
            player.ReturnCamera();
        }
    }

    public void OnFocus()
    {
    }

    public void OnUnfocus()
    {
    }

    internal void SetupTVText(string text)
    {
        // Create UI for text if it doesn't exist
        Control textContainer = SubViewport.GetNodeOrNull<Control>("TextContainer");
        if (textContainer == null)
        {
            // Create container
            textContainer = new Control();
            textContainer.Name = "TextContainer";
            textContainer.SetSize(new Vector2(SubViewport.Size.X, SubViewport.Size.Y));
            SubViewport.AddChild(textContainer);

            // Optional: Add a background
            ColorRect background = new ColorRect();
            background.Name = "Background";
            background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            background.Color = new Color(0, 0, 0, 0.7f); // Semi-transparent black
            textContainer.AddChild(background);
            
            // Create label
            Label label = new Label();
            label.Name = "TVLabel";
            label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.AddThemeFontSizeOverride("font_size", 48);
            label.AddThemeColorOverride("font_color", Colors.White);
            label.AddThemeConstantOverride("outline_size", 4);
            label.AddThemeColorOverride("font_outline_color", Colors.Black);

            textContainer.AddChild(label);

        }

        // Update text
        Label tvLabel = textContainer.GetNode<Label>("TVLabel");
        tvLabel.Text = text;

        // Create and assign material to screen
        StandardMaterial3D screenMaterial = new StandardMaterial3D
        {
            AlbedoTexture = SubViewport.GetTexture(),
            EmissionEnabled = true,
            Emission = new Color(1, 1, 1),
            EmissionEnergyMultiplier = 1.0f
        };
        Screen.MaterialOverride = screenMaterial;
    }
    internal void UpdateTVText(string newText)
    {
        Control textContainer = SubViewport.GetNodeOrNull<Control>("TextContainer");

        if (textContainer != null)
        {
            Label tvLabel = textContainer.GetNode<Label>("TVLabel");
            tvLabel.Text = newText;
        }
    }
}
