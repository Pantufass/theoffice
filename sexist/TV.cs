using Godot;
using System;

public partial class TV : Interactable
{
	[Export] public MeshInstance3D Screen;
	[Export] public SubViewport SubViewport;
	[Export] public Texture2D BananaImage; 
	[Export] public Texture2D PaitingImage; 
	[Export] public Texture2D ShirtImage; 
	[Export] public Texture2D ManImage; 
	private Texture2D current; 
	
	private StandardMaterial3D _screenMaterial;
	private Control _imageContainer;
	private TextureRect _tvImage;
	private Timer _imageTimer;

	private bool tvOn = false;

	public override void _Ready()
	{
		base._Ready();
		if(Screen == null) Screen = GetNode<MeshInstance3D>("Screen");
		if(SubViewport == null) SubViewport = GetNode<SubViewport>("SubViewport");
		
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
		if(!tvOn)
		{
			player.SetHint("TV is off") ;
			return;
		}
		
		if (_tvImage.Visible)
		{
			HideImage();
		}
		else
		{
			ShowImage();
		}
	}
	
	internal void UpdateImage(int step)
	{
		if (step == 1) current = PaitingImage;
		else if (step == 2) current = ShirtImage;
		else if (step == 3) current = BananaImage;
		else if (step == 4) current = ManImage;
		else
		{
			GD.PrintErr($"Invalid step {step} for TV image update");
			return;
		}
		
		GD.Print($"TV image updated for step {step}");
		HideImage();
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

	internal void HideImage()
	{
		GD.Print("Hiding TV image");

		if (_tvImage != null)
		{
			_tvImage.Visible = false;
		}

		_imageTimer?.Stop();
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

	public void TurnOn()
	{
		tvOn = true;
	}

	public override void OnFocus(PlayerCharacter player)
	{
		//base.OnFocus(player);
	}
	public override void OnUnfocus(PlayerCharacter player)
	{
		//base.OnUnfocus(player);
	}

}
