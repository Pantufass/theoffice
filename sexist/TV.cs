using Godot;
using System;

public partial class TV : Node3D, IInteractable
{
    [Export] public MeshInstance3D Screen;
    [Export] public SubViewport SubViewport;
    
    [Export] public Texture2D BananaImage; 
    [Export] public Texture2D PaitingImage; 
    [Export] public Texture2D ShirtImage; 
    private Texture2D current; 
    
    private StandardMaterial3D _screenMaterial;
    private Control _imageContainer;
    private TextureRect _tvImage;
    private Timer _imageTimer;

    public override void _Ready()
    {
        // Get references
        if(Screen == null) Screen = GetNode<MeshInstance3D>("Screen");
        if(SubViewport == null) SubViewport = GetNode<SubViewport>("SubViewport");
        
        // Configure the viewport
        ConfigureViewport();

        // Create and store the material

        _screenMaterial = new StandardMaterial3D
        {
            AlbedoTexture = SubViewport.GetTexture(),
            EmissionEnabled = true,
            Emission = new Color(1, 1, 1),
            EmissionEnergyMultiplier = 1.0f,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
        };


        Screen.MaterialOverride = _screenMaterial;
        
        SetupImageDisplay();
        
        HideImage();

        current = PaitingImage;
        
        GD.Print("TV initialized - ready to show images");
    }
    
    private void ConfigureViewport()
    {
        if (SubViewport == null) return;
        
        SubViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        SubViewport.RenderTargetClearMode = SubViewport.ClearMode.Always;
        
        if (SubViewport.Size == Vector2I.Zero)
        {
            SubViewport.Size = new Vector2I(1920, 1080);
        }
        
        SubViewport.Disable3D = true;
        SubViewport.HandleInputLocally = false;
    }

    private void SetupImageDisplay()
    {
        // Create container for image
        _imageContainer = SubViewport.GetNodeOrNull<Control>("ImageContainer");
        if (_imageContainer == null)
        {
            _imageContainer = new Control();
            _imageContainer.Name = "ImageContainer";
            _imageContainer.SetSize(new Vector2(SubViewport.Size.X, SubViewport.Size.Y));
            SubViewport.AddChild(_imageContainer);

            // Create image display
            _tvImage = new TextureRect();
            _tvImage.Name = "TVImage";
            _tvImage.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _tvImage.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            _tvImage.ExpandMode = TextureRect.ExpandModeEnum.FitWidth;
            _tvImage.Visible = false;
            
            _imageContainer.AddChild(_tvImage);
            
            GD.Print("Created image display");
        }
        else
        {
            _tvImage = _imageContainer.GetNode<TextureRect>("TVImage");
        }
    }

    public void Interact(PlayerCharacter player)
    {
        if (_tvImage.Visible)
        {
            HideImage();
        }
        else
        {
            ShowImage();
        }
    }

    public void OnFocus(PlayerCharacter player)
    {
        if (_screenMaterial != null)
        {
            _screenMaterial.EmissionEnergyMultiplier = 1.5f;
        }
        
        player.SetHint("Press E to interact with TV");
    }

    public void OnUnfocus(PlayerCharacter player)
    {
        if (_screenMaterial != null)
        {
            _screenMaterial.EmissionEnergyMultiplier = 1.0f;
        }
        
        player.SetHint("");
    }
    
    internal void UpdateImage(int step)
    {
        if (step == 1) current = PaitingImage;
        else if (step == 2) current = ShirtImage;
        else if (step == 3) current = BananaImage;
        else
        {
            GD.PrintErr($"Invalid step {step} for TV image update");
            return;
        }
        
        GD.Print($"TV image updated for step {step}");
        HideImage();
    }
    // Show the image (called from player when zoom completes)
    internal void ShowImage()
    {
        ShowImage(current);
    }
    
    internal void ShowImage(Texture2D image)
    {
        if (image == null)
        {
            GD.PrintErr("No image to display!");
            return;
        }
        
        GD.Print($"Showing image on TV: {image.ResourcePath}");
        
        // Make sure UI exists
        if (_tvImage == null)
        {
            SetupImageDisplay();
        }
        
        // Show image
        _tvImage.Visible = true;
        _tvImage.Texture = image;
        
        
        // Force viewport to update
        ForceViewportUpdate();
    }
    
    internal void HideImage()
    {
        GD.Print("Hiding TV image");
        
        if (_tvImage != null)
        {
            _tvImage.Visible = false;
        }
        
        ForceViewportUpdate();
    }
    
    private void OnImageTimerTimeout()
    {
        HideImage();
    }
    
    private void ForceViewportUpdate()
    {
        if (SubViewport != null)
        {
            SubViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
            
            if (_screenMaterial != null)
            {
                _screenMaterial.AlbedoTexture = SubViewport.GetTexture();
            }
            
            CallDeferred("RestoreViewportMode");
        }
    }
    
    private void RestoreViewportMode()
    {
        if (SubViewport != null)
        {
            SubViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        }
    }
}