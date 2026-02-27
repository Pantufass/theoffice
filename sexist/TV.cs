using Godot;
using System;

public partial class TV : Node3D, IInteractable
{
    [Export] public MeshInstance3D Screen;
    [Export] public SubViewport SubViewport;
    [Export] public Color TextColor = Colors.White;
    [Export] public int FontSize = 48;
    
    private StandardMaterial3D _screenMaterial;
    private Control _textContainer;
    private Label _tvLabel;
    private ColorRect _background;

    public override void _Ready()
    {
        // Get references
        if(Screen == null) Screen = GetNode<MeshInstance3D>("Screen");
        if(SubViewport == null) SubViewport = GetNode<SubViewport>("SubViewport");
        
        // IMPORTANT: Configure the viewport properly
        ConfigureViewport();
        
        // Create and store the material
        _screenMaterial = new StandardMaterial3D();
        
        // Make sure texture is assigned
        var viewportTexture = SubViewport.GetTexture();
        _screenMaterial.AlbedoTexture = viewportTexture;
        
        // Add emission for glow effect
        _screenMaterial.EmissionEnabled = true;
        _screenMaterial.Emission = new Color(1, 1, 1);
        _screenMaterial.EmissionEnergyMultiplier = 1.0f;
        
        // Disable shading to show full brightness
        _screenMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        
        // Apply material to screen
        Screen.MaterialOverride = _screenMaterial;
        
        // Setup the UI
        SetupTVUI();
        
        // Set initial text
        UpdateTVText("Booting up");
        
        GD.Print("TV initialized with viewport texture");
    }
    
    private void ConfigureViewport()
    {
        if (SubViewport == null) return;
        
        // CRITICAL: Set these properties for the viewport to render properly
        SubViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always; // Or .Once if you want manual updates
        SubViewport.RenderTargetClearMode = SubViewport.ClearMode.Always; // Clear before rendering
        
        // Set a reasonable size if not set
        if (SubViewport.Size == Vector2I.Zero)
        {
            SubViewport.Size = new Vector2I(1920, 1080);
            GD.Print("Set default viewport size to 1920x1080");
        }
        
        // Disable 3D rendering if not needed
        SubViewport.Disable3D = true;
        
        // Enable 2D rendering
        SubViewport.HandleInputLocally = false;
        
        GD.Print($"Viewport configured - Size: {SubViewport.Size}, UpdateMode: {SubViewport.RenderTargetUpdateMode}");
    }

    private void SetupTVUI()
    {
        // Create UI for text if it doesn't exist
        _textContainer = SubViewport.GetNodeOrNull<Control>("TextContainer");
        if (_textContainer == null)
        {
            // Create container
            _textContainer = new Control();
            _textContainer.Name = "TextContainer";
            _textContainer.SetSize(new Vector2(SubViewport.Size.X, SubViewport.Size.Y));
            SubViewport.AddChild(_textContainer);
            GD.Print("Created TextContainer");

            // Add a background
            _background = new ColorRect();
            _background.Name = "Background";
            _background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _background.Color = new Color(0, 0, 0, 0.7f); // Semi-transparent black
            _textContainer.AddChild(_background);
            GD.Print("Created Background");

            // Create label
            _tvLabel = new Label();
            _tvLabel.Name = "TVLabel";
            _tvLabel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _tvLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _tvLabel.VerticalAlignment = VerticalAlignment.Center;
            _tvLabel.AddThemeFontSizeOverride("font_size", FontSize);
            _tvLabel.AddThemeColorOverride("font_color", TextColor);
            _tvLabel.AddThemeConstantOverride("outline_size", 4);
            _tvLabel.AddThemeColorOverride("font_outline_color", Colors.Black);

            _textContainer.AddChild(_tvLabel);
            GD.Print("Created Label");
        }
        else
        {
            _tvLabel = _textContainer.GetNode<Label>("TVLabel");
            _background = _textContainer.GetNode<ColorRect>("Background");
            GD.Print("Found existing UI elements");
        }
    }

    public void Interact(PlayerCharacter player)
    {
    }

    public void OnFocus(PlayerCharacter player)
    {
        // Highlight effect
        if (_screenMaterial != null)
        {
            _screenMaterial.EmissionEnergyMultiplier = 1.5f;
        }
    }

    public void OnUnfocus(PlayerCharacter player)
    {
    }

    internal void UpdateTVText(string newText)
    {
        // Make sure the label exists
        if (_tvLabel == null)
        {
            SetupTVUI();
        }
        
        if (_tvLabel != null)
        {
            _tvLabel.Text = newText;
            GD.Print($"TV text updated to: '{newText}'");
        }
        
        // Force viewport to update
        if (SubViewport != null)
        {
            SubViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
            
            // Force material to refresh texture
            if (_screenMaterial != null)
            {
                _screenMaterial.AlbedoTexture = SubViewport.GetTexture();
            }
        }
    }
    
    // Call this to show just the viewport content without text
    internal void HideText()
    {
        if (_textContainer != null)
        {
            _textContainer.Visible = false;
        }
    }
    
    internal void ShowText()
    {
        if (_textContainer != null)
        {
            _textContainer.Visible = true;
        }
    }
}