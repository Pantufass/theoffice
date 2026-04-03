using Godot;
using System;

public partial class Computer : Interactable
{
	[Export] public MeshInstance3D Screen;
	[Export] public SubViewport SubViewport;
	[Export] public Texture2D BackgroundImage; 
	private Texture2D current; 
	
	private StandardMaterial3D _screenMaterial;
	private Control _imageContainer;
	private TextureRect _tvImage;
	private Timer _imageTimer;
    private Label _textDisplay;

	public override void _Ready()
	{
		base._Ready();
		if(Screen == null) Screen = GetNode<MeshInstance3D>("Screen");
		if(SubViewport == null) SubViewport = Screen.GetNode<SubViewport>("SubViewport");
		
		ConfigureViewport();

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
        SetupTextDisplay();
		
		_tvImage.Texture = BackgroundImage;
        _tvImage.Visible = true;
	}

    private void SetupTextDisplay()
    {
        _textDisplay = new Label
        {
            Name = "TextDisplay",
            Visible = false,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.Word
        };
        _textDisplay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _imageContainer.AddChild(_textDisplay);
        _textDisplay.ZIndex = 1; 
        _textDisplay.Text = "TEXT DISPLAY";
        _textDisplay.AddThemeColorOverride("font_color", new Color(1, 1, 1));
        _textDisplay.AddThemeColorOverride("font_color_shadow", new Color(0, 0, 0, 0.5f));
        _textDisplay.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0, 0.5f),
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 5,
            ContentMarginBottom = 5
        });
        _textDisplay.AddThemeFontSizeOverride("font_size", 44);
    }

    public void ShowText(string text)
    {
        if (_textDisplay == null)
            SetupTextDisplay();

        _tvImage.Visible = false;
        _textDisplay.Text = text;
        _textDisplay.Visible = true;
        ForceViewportUpdate();
    }
	
	private void ConfigureViewport()
	{
		if (SubViewport == null) throw new Exception("SubViewport not assigned to Computer");
		
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
			_imageContainer = new Control
			{
				Name = "ImageContainer"
			};
			_imageContainer.SetSize(new Vector2(SubViewport.Size.X, SubViewport.Size.Y));
			SubViewport.AddChild(_imageContainer);

			// Create image display
			_tvImage = new TextureRect
			{
				Name = "TVImage",
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				ExpandMode = TextureRect.ExpandModeEnum.FitWidth,
				Visible = false
			};
			_tvImage.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			
			_imageContainer.AddChild(_tvImage);
			
			GD.Print("Created image display");
		}
		else
		{
			_tvImage = _imageContainer.GetNode<TextureRect>("TVImage");
		}
	}

	public override void Interact(PlayerCharacter player)
	{
		base.Interact(player);
        //TODO 


	}

	internal void ShowImage()
	{
		if (current == null)
		{
			GD.PrintErr("No image to display!");
			return;
		}

		GD.Print("Showing image on TV");

		if (_tvImage == null)
		{
			SetupImageDisplay();
		}

		if (_tvImage != null)
		{
			_tvImage.Visible = true;
			_tvImage.Texture = current;

			ForceViewportUpdate();
		}
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
    public override void _ExitTree()
    {
        base._ExitTree();
        
        // Clean up material to prevent memory leaks
        if (_screenMaterial != null)
        {
            _screenMaterial.Dispose();
        }
    }

}
