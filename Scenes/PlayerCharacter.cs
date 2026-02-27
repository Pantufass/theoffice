using Godot;
using GodotPlugins.Game;
using System;

public partial class PlayerCharacter : CharacterBody3D
{
	// ────────── Nodes ──────────
	private Godot.Range sprintBar;
	[Export] public Node3D Hand;
	public Item currentItem = null;

	[Export] public Camera3D SubviewportCamera;
	[Export] public Camera3D MainCamera;
	[Export] public AnimationTree AnimationTree;

	// ────────── Camera ──────────
	private Vector2 cameraRotation = Vector2.Zero;
	private float mouseSensitivity = 0.001f;
	[Export] public float InteractDistance = 4f;

	// ────────── Crouch ──────────
	private bool crouched = false;
	private bool crouchBlocked = false;

	[Export] public bool EnableCrouch = true;
	[Export] public bool CrouchToggle = false;
	[Export] public ShapeCast3D CrouchCollision;
	[Export] public float CrouchSpeedReduction = 2f;
	[Export] public float CrouchBlendSpeed = 0.2f;

	private const int GROUND_CROUCH = -1;
	private const int STANDING = 0;
	private const int AIR_CROUCH = 1;

	private Tween leanTween;

	private const int LEFT = 1;
	private const int CENTRE = 0;
	private const int RIGHT = -1;

	// ────────── Sprint ──────────
	[Export] public bool EnableSprint = true;
	[Export] public Timer SprintTimer;
	[Export] public float SprintCooldownTime = 3f;
	[Export] public float SprintTime = 1f;
	[Export] public float SprintReplenishRate = 0.30f;
	[Export] public float Acceleration = 120f;
	[Export] public float AirAccelerationModifier = 0.1f;

	private bool sprintOnCooldown = false;
	private float sprintTimeRemaining;

	private const float NORMAL_SPEED = 1.5f;
	[Export] public float SprintSpeed = 1.8f;
	[Export] public float WalkSpeed = 0.5f;

	private float speedModifier = NORMAL_SPEED;

	// ────────── Jump ──────────
	[Export] public Timer CoyoteTimer;
	[Export] public float JumpPeakTime = 0.5f;
	[Export] public float JumpFallTime = 0.5f;
	[Export] public float JumpHeight = 2f;
	[Export] public float JumpDistance = 4f;
	[Export] public float CoyoteTime = 0.1f;
	[Export] public float JumpBufferTime = 0.2f;
	[Export] public float MaxFallSpeed = 6f;
	public IInteractable CurrentInteractable { get; private set; }

	private Vector3 direction = Vector3.Zero;
	private float jumpGravity;
	private float fallGravity;
	private float jumpVelocity;
	private float baseSpeed;
	private float speed;

	private bool jumpAvailable = true;
	private bool jumpBuffer = false;

	public static Action Die;
	public Action<Item> SetCurrentPickupable;
    
    // Camera and sitting variables
    private Transform3D originalCameraTransform;
    private Transform3D targetTVTransform;
    [Export] public Node3D TV;
    private bool sitting = false;
    public bool Sitting {get => sitting;}
    private Tween currentTween;    
    private bool isCameraMoving = false;
    
    // Store original player transform BEFORE sitting
    private Vector3 _originalPlayerPosition;
    private Basis _originalPlayerRotation;
    private Vector3 _originalCameraPosition;
    private Basis _originalCameraRotation;
    
    private bool movementEnabled = true;

    [Export] public CanvasLayer HUD;
    [Export] public Label DialogText;


	// ────────── Ready ──────────
	public override void _Ready()
	{
        if(MainCamera == null) MainCamera = GetNode<Camera3D>("MainCamera");
		if (AnimationTree == null) AnimationTree = GetNode<AnimationTree>("AnimationTree");
		sprintBar = GetNode<CanvasLayer>("HUD").GetNode<Godot.Range>("SprintBar");
		sprintTimeRemaining = SprintTime;

		if(SprintTimer == null)
		{
			SprintTimer = GetNode<Timer>("SprintTimer");
		}
		if(CoyoteTimer == null)
		{
			CoyoteTimer = GetNode<Timer>("CoyoteTimer");
		}
		SprintTimer.Timeout += OnSprintTimerTimeout;
		CoyoteTimer.Timeout += OnCoyoteTimerTimeout;

		UpdateCameraRotation();
		Input.MouseMode = Input.MouseModeEnum.Captured;
		CalculateMovementParameters();

        if(HUD == null) HUD = GetNode<CanvasLayer>("HUD");
		if(Hand == null) Hand = GetNode<Node3D>("%Hand");
        if(DialogText == null) DialogText = HUD.GetNode<Label>("Dialog");

		SetCurrentPickupable += EquipItem;
        
        // Store initial camera transform relative to player
        UpdateOriginalTransforms();
	}
    
    private void UpdateOriginalTransforms()
    {
        _originalPlayerPosition = GlobalPosition;
        _originalPlayerRotation = GlobalTransform.Basis;
        _originalCameraPosition = MainCamera.Position;
        _originalCameraRotation = MainCamera.Transform.Basis;
    }
    
	public override void _ExitTree()
	{
		Die -= PlayerIsKill;
		SetCurrentPickupable -= EquipItem;
	}

	public void EquipItem(Item item)
	{
		if( currentItem != null)
		{
			currentItem.OnDrop();
		}
		currentItem = item;
		(item as Node3D).GetParent()?.RemoveChild(item as Node3D);
		Hand.AddChild(currentItem as Node3D);

		(currentItem as Node3D).Transform = Transform3D.Identity;
		item.OnPickup();
	}

	public void PlayerIsKill()
	{
		GD.Print("No more player");
		SetPhysicsProcess(false);

		var tween = GetTree().CreateTween();

		tween.TweenProperty(
			MainCamera,
			"rotation:x",
			MainCamera.Rotation.X + 1.2f,
			0.6f
		);

		tween.TweenProperty(
			MainCamera,
			"position:y",
			MainCamera.Position.Y - 0.6f,
			0.6f
		);
		Input.MouseMode = Input.MouseModeEnum.Visible;
		//GameOver.End?.Invoke();
	}

	// ────────── Input ──────────
	public override void _Input(InputEvent input)
	{
		if(Input.IsActionJustPressed("Interact") && CurrentInteractable != null)
		{
			CurrentInteractable.Interact(this);
			if(currentItem != null)
			{
				currentItem.Use();
			}
		}
		// ───── Toggle Mouse Mode ─────
		if (input.IsActionPressed("ui_cancel"))
		{
			if (Input.MouseMode == Input.MouseModeEnum.Captured)
				Input.MouseMode = Input.MouseModeEnum.Visible;
			else
				Input.MouseMode = Input.MouseModeEnum.Captured;
		}

		// ───── Mouse Look ─────
		if (input is InputEventMouseMotion motionEvent && !sitting && !isCameraMoving)
		{
			Vector2 mouseMovement = motionEvent.Relative * mouseSensitivity;
			CameraLook(mouseMovement);
		}

		// ───── Crouch ─────
		if (EnableCrouch && !sitting)
		{
			if (input.IsActionPressed("crouch"))
				ToggleCrouch();

			if (input.IsActionReleased("crouch"))
			{
				if (!CrouchToggle && crouched)
					ToggleCrouch();
			}
		}

		// ───── Sprint / Walk ─────
		if (EnableSprint && !sitting)
		{
			// Released sprint or walk
			if (Input.IsActionJustReleased("sprint") || Input.IsActionJustReleased("walk"))
			{
				if (!(Input.IsActionPressed("walk") || Input.IsActionPressed("sprint")))
				{
					speedModifier = NORMAL_SPEED;
					ExitSprint();
				}
			}

			// Press sprint
			if (Input.IsActionJustPressed("sprint") && !crouched)
			{
				if (!sprintOnCooldown)
				{
					speedModifier = SprintSpeed;
					SprintTimer.Start(sprintTimeRemaining);
				}
			}

			// Press walk
			if (Input.IsActionJustPressed("walk") && !crouched)
			{
				speedModifier = WalkSpeed;
			}
	    }
    }

	// ────────── Camera ──────────
	private void UpdateCameraRotation()
	{
		Vector3 rot = Rotation;
		cameraRotation.X = rot.Y;
		cameraRotation.Y = rot.X;
	}
	private void ExitSprint()
	{
		if (!SprintTimer.IsStopped())
		{
			sprintTimeRemaining = (float)SprintTimer.TimeLeft;
			SprintTimer.Stop();
		}
	}


	private void CameraLook(Vector2 movement)
	{
		cameraRotation += movement;

		Transform = new Transform3D(Basis.Identity, Transform.Origin);
		MainCamera.Transform = new Transform3D(Basis.Identity, MainCamera.Transform.Origin);

		RotateObjectLocal(Vector3.Up, -cameraRotation.X);
		MainCamera.RotateObjectLocal(Vector3.Right, -cameraRotation.Y);

		cameraRotation.Y = Mathf.Clamp(cameraRotation.Y, -1.5f, 1.2f);
	}

	private void ToggleCrouch()
	{
		if (CrouchCollision.IsColliding())
		{
			crouchBlocked = true;
			return;
		}

		int blend = crouched
			? STANDING
			: IsOnFloor() ? GROUND_CROUCH : AIR_CROUCH;

		Tween tween = GetTree().CreateTween();
		tween.TweenProperty(AnimationTree,
			"parameters/Crouch_Blend/blend_amount",
			blend,
			CrouchBlendSpeed);

		crouched = !crouched;
	}

	private void SprintReplenish(float delta)
	{
		float sprintBarValue;
		if (!sprintOnCooldown && speedModifier != SprintSpeed)
		{
			if (IsOnFloor())
			{
				sprintTimeRemaining = Mathf.MoveToward(
					sprintTimeRemaining,
					SprintTime,
					delta * SprintReplenishRate
				);
			}

			sprintBarValue = (sprintTimeRemaining / SprintTime) * 100f;
		}
		else
		{
			sprintBarValue = ((float)SprintTimer.TimeLeft / SprintTime) * 100f;
		}

		sprintBar.Value = Mathf.Clamp(sprintBarValue, 0f, 100f);

		if (Mathf.IsEqualApprox(sprintBarValue, 100f))
			sprintBar.Hide();
		else
			sprintBar.Show();
	}

	public override void _Process(double delta)
	{
        if(sitting) return;
		CheckInteractable(delta);
		if(SubviewportCamera != null && MainCamera != null)
		{
			SubviewportCamera.GlobalTransform = MainCamera.GlobalTransform;
		}
	}
	
	// ────────── Physics ──────────
	public override void _PhysicsProcess(double delta)
	{
		float d = (float)delta;

		SprintReplenish(d);
		
		// ───── Crouch unblock ─────
		if (crouched && crouchBlocked)
		{
			if (!CrouchCollision.IsColliding())
			{
				crouchBlocked = false;

				if (!Input.IsActionPressed("crouch") && !CrouchToggle)
					ToggleCrouch();
			}
		}
		
        // Skip movement if sitting
        if(sitting) return;

		float accelerationValue;

		// ───── Gravity & air logic ─────
		if (!IsOnFloor())
		{
			accelerationValue = Acceleration * AirAccelerationModifier;

			if (CoyoteTimer.IsStopped())
				CoyoteTimer.Start(CoyoteTime);

			if (Velocity.Y > 0)
				Velocity = new Vector3(
					Velocity.X,
					Velocity.Y - jumpGravity * d,
					Velocity.Z
				);
			else
				Velocity = new Vector3(
					Velocity.X,
					Velocity.Y - fallGravity * d ,
					Velocity.Z
				);
			Velocity = new Vector3(
				Velocity.X,
				Mathf.Max(Velocity.Y, -MaxFallSpeed),
				Velocity.Z
			);
		}
		else
		{
			accelerationValue = Acceleration;
			jumpAvailable = true;
			CoyoteTimer.Stop();

			speed = (baseSpeed / Mathf.Max(crouched ? CrouchSpeedReduction : 1f, 1f))* speedModifier;

			if (jumpBuffer)
			{
				Jump();
				jumpBuffer = false;
			}
		}

        if(!movementEnabled) return;

		// ───── Jump input ─────
		if (Input.IsActionJustPressed("ui_accept"))
		{
			if (jumpAvailable)
			{
				if (crouched)
				{
					ToggleCrouch();
				}
				else
				{
					Jump();
				}
			}
			else
			{
				jumpBuffer = true;
				GetTree()
					.CreateTimer(JumpBufferTime)
					.Timeout += OnJumpBufferTimeout;
			}
		}

		// ───── Movement ─────
		Vector2 inputDir = Input.GetVector("left", "right", "up", "down");
		if(IsOnFloor())
		{
			direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		}

		Velocity = new Vector3(
			Mathf.MoveToward(Velocity.X, direction.X * speed, accelerationValue * d),
			Velocity.Y,
			Mathf.MoveToward(Velocity.Z, direction.Z * speed, accelerationValue * d)
		);

		MoveAndSlide();
	}


	private void OnSprintTimerTimeout()
	{
		sprintOnCooldown = true;

		GetTree()
			.CreateTimer(SprintCooldownTime)
			.Timeout += OnSprintCooldownTimeout;

		speedModifier = NORMAL_SPEED;
		sprintTimeRemaining = 0f;
	}

	private void OnSprintCooldownTimeout()
	{
		sprintOnCooldown = false;
	}

	private void OnCoyoteTimerTimeout()
	{
		jumpAvailable = false;
	}
	private void OnJumpBufferTimeout()
	{
		jumpBuffer = false;
	}


 	private void CalculateMovementParameters()
	{
		jumpGravity = (2 * JumpHeight) / Mathf.Pow(JumpPeakTime, 2);
		fallGravity = (2 * JumpHeight) / Mathf.Pow(JumpFallTime, 2);
		jumpVelocity = jumpGravity * JumpPeakTime;
		baseSpeed = JumpDistance / (JumpPeakTime + JumpFallTime);
		speed = baseSpeed;
	}

	private void Jump()
	{
		Velocity = new Vector3(Velocity.X, jumpVelocity, Velocity.Z);
		jumpAvailable = false;
	}

	private void SetInteractable(IInteractable interactable)
	{
		if (CurrentInteractable == interactable)
			return;

		CurrentInteractable?.OnUnfocus(this);
		CurrentInteractable = interactable;
		CurrentInteractable?.OnFocus(this);
	}

	private void ClearInteractable()
	{
		SetInteractable(null);
	}
	
	private void CheckInteractable( double delta)
	{
		if (MainCamera == null)
			return;

		Vector2 viewportCenter = GetViewport().GetVisibleRect().Size / 2f;

		Vector3 rayOrigin = MainCamera.ProjectRayOrigin(viewportCenter);
		Vector3 rayDirection = MainCamera.ProjectRayNormal(viewportCenter);

		Vector3 rayEnd = rayOrigin + rayDirection * InteractDistance;

		var query = PhysicsRayQueryParameters3D.Create(
			rayOrigin,
			rayEnd
		);

		query.CollideWithAreas = true;
		query.CollideWithBodies = true;
		query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };

		var space = GetWorld3D().DirectSpaceState;
		var result = space.IntersectRay(query);

		if (result.Count == 0)
		{
			ClearInteractable();
			return;
		}

		var collider = result["collider"].As<Node>();
		Node hit = collider;

		IInteractable found = null;

		while (hit != null)
		{
			if (hit is IInteractable interactable)
			{
				found = interactable;
				break;
			}

			hit = hit.GetParent();
		}

		if (found != null)
		{
			if (CurrentInteractable != found)
			{
				ClearInteractable();
				SetInteractable(found);
			}
		}
		else
		{
			ClearInteractable();
		}
	}

    public void SetText(string text)
    {
        DialogText.Text = text;
    }

    internal void ZoomToTV(Transform3D tvTransform)
    {   
        if (!sitting) 
        {
            GD.Print("Cannot zoom - player not sitting");
            return;
        }
        
        GD.Print("=== Zoom To TV ===");
        GD.Print($"TV Transform - Origin: {tvTransform.Origin}, Basis: {tvTransform.Basis}");
        GD.Print($"Camera Current - Origin: {MainCamera.GlobalPosition}");
        
        // Kill any existing tween
        currentTween?.Kill();
        
        // Store original camera position
        originalCameraTransform = new Transform3D(
            MainCamera.GlobalTransform.Basis,
            MainCamera.GlobalTransform.Origin
        );

        // Calculate target position and transform
        float zoomDistance = 2.0f;
        Vector3 tvForward = -tvTransform.Basis.Z;
        Vector3 tvUp = tvTransform.Basis.Y;
        
        // TV position (assuming this is at the base of the TV)
        Vector3 tvBase = tvTransform.Origin;
        
        // TV center height (adjust based on your TV's actual height)
        float tvHeight = 1.2f; // Height of TV center from floor
        float eyeLevel = 1.6f; // Camera eye level when sitting
        
        // Calculate TV center position
        Vector3 tvCenter = new Vector3(tvBase.X, tvHeight, tvBase.Z);
        
        // OPTION: Position camera between eye level and TV height
        // 0.0 = eye level, 1.0 = TV center level
        float blendFactor = 0.7f; // 0.7 = 70% toward TV level, 30% eye level
        
        float cameraHeight = Mathf.Lerp(eyeLevel, tvHeight, blendFactor);
        
        // Position camera at the blended height
        Vector3 targetPosition = new Vector3(
            tvBase.X + (tvForward.X * zoomDistance),
            cameraHeight,
            tvBase.Z + (tvForward.Z * zoomDistance)
        );
        
        // Look at TV center (or slightly below for natural gaze)
        Vector3 lookAtPoint = new Vector3(
            tvCenter.X,
            tvCenter.Y - 0.1f, // Slight downward tilt
            tvCenter.Z
        );
        
        GD.Print($"TV Base: {tvBase}");
        GD.Print($"TV Center: {tvCenter}");
        GD.Print($"Eye Level: {eyeLevel}");
        GD.Print($"Blend Factor: {blendFactor} -> Camera Height: {cameraHeight}");
        GD.Print($"Target Position: {targetPosition}");
        GD.Print($"Looking at: {lookAtPoint}");
        
        // Create transform that looks at the TV
        targetTVTransform = new Transform3D(
            Basis.LookingAt(lookAtPoint - targetPosition, Vector3.Up),
            targetPosition
        );
        
        // Disable player input
        SetPlayerInputEnabled(false);
        
        // Create tween
        currentTween = CreateTween();
        currentTween.SetParallel(true);
        
        currentTween.TweenProperty(MainCamera, "global_position", targetPosition, 0.8f)
             .SetEase(Tween.EaseType.Out)
             .SetTrans(Tween.TransitionType.Quad);
        
        currentTween.TweenProperty(MainCamera, "global_transform:basis", targetTVTransform.Basis, 0.8f)
             .SetEase(Tween.EaseType.Out)
             .SetTrans(Tween.TransitionType.Quad);
        
        // Set up completion callback
        currentTween.Finished += OnZoomToTVComplete;
    }
    
    private void OnZoomToTVComplete()
    {
        // Force exact position and rotation
        MainCamera.GlobalPosition = targetTVTransform.Origin;
        MainCamera.GlobalTransform = targetTVTransform;
        
        isCameraMoving = false;
        
        GD.Print($"Camera reached TV - Position: {MainCamera.GlobalPosition}");
    }
    
    internal void ReturnCamera()
    {
        if (!sitting) 
        {
            GD.Print("Cannot return camera - player not sitting");
            return;
        }

        if (isCameraMoving) return;
        
        GD.Print("=== Return Camera ===");
        
        isCameraMoving = true;
        
        currentTween?.Kill();
        
        currentTween = CreateTween();
        currentTween.SetParallel(true);
        
        // Return to the camera position that was stored BEFORE zooming
        currentTween.TweenProperty(MainCamera, "global_position", originalCameraTransform.Origin, 0.8f)
             .SetEase(Tween.EaseType.Out)
             .SetTrans(Tween.TransitionType.Quad);
        
        currentTween.TweenProperty(MainCamera, "global_transform:basis", originalCameraTransform.Basis, 0.8f)
             .SetEase(Tween.EaseType.Out)
             .SetTrans(Tween.TransitionType.Quad);
        
        // Set up completion callback
        currentTween.Finished += OnReturnCameraComplete;
        
    }
    
    private void OnReturnCameraComplete()
    {
        // Force exact original camera position
        MainCamera.GlobalPosition = originalCameraTransform.Origin;
        MainCamera.GlobalTransform = originalCameraTransform;
        
        isCameraMoving = false;
        
        GD.Print($"Camera returned - Position: {MainCamera.GlobalPosition}");
    }

    private void SetPlayerInputEnabled(bool enabled)
    {
        // This is for camera movement during TV zoom - keep as is
        movementEnabled = enabled;
        GD.Print($"Camera input {(enabled ? "enabled" : "disabled")}");
    }

    public void SitOnCouch(Vector3 targetPosition, Basis targetRotation, float duration, Node3D tvTarget = null)
    {
        if (sitting) return;

        GD.Print($"Player sitting at position: {targetPosition}");

        // Store original player transform BEFORE moving
        _originalPlayerPosition = GlobalPosition;
        _originalPlayerRotation = GlobalTransform.Basis;
        
        // Store original camera position relative to player
        _originalCameraPosition = MainCamera.Position;
        _originalCameraRotation = MainCamera.Transform.Basis;

        // Kill any existing tween
        currentTween?.Kill();

        // Disable player movement
        SetMovementEnabled(false);
        sitting = true;

        // Create tween for smooth sitting
        currentTween = CreateTween();
        currentTween.SetParallel(true);

        currentTween.TweenProperty(this, "global_position", targetPosition, duration)
             .SetEase(Tween.EaseType.Out)
             .SetTrans(Tween.TransitionType.Quad);

        currentTween.TweenProperty(this, "global_transform:basis", targetRotation, duration)
             .SetEase(Tween.EaseType.Out)
             .SetTrans(Tween.TransitionType.Quad);

        currentTween.Finished += () =>
        {
            // Snap to exact position
            GlobalPosition = targetPosition;
            GlobalTransform = new Transform3D(targetRotation, targetPosition);
            
            GD.Print("Player is now sitting");
            
            // Zoom to TV if available
            if (tvTarget != null)
            {
                // Small delay before zooming
                var timer = GetTree().CreateTimer(0.2f);
                timer.Timeout += () => {
                    if (sitting) // Make sure we're still sitting
                    {
                        ZoomToTV(tvTarget.GlobalTransform);
                    }
                };
            }
        };
    }

    public void StandFromCouch(Vector3 targetPosition, float duration)
    {
        if (!sitting) return;

        GD.Print($"Player standing at position: {targetPosition}");

        if (!MainCamera.GlobalPosition.IsEqualApprox(originalCameraTransform.Origin))
        {
            GD.Print("Camera needs to return first");
            GD.Print($"Current: {MainCamera.GlobalPosition}, Target: {originalCameraTransform.Origin}");

            ReturnCamera();

            // Use a loop to check when camera is done moving
            void CheckCameraDone()
            {
                if (!isCameraMoving && sitting)
                {
                    GD.Print("Camera done moving, now standing");
                    PerformStand(targetPosition, duration);
                }
                else
                {
                    GD.Print($"Waiting for camera... isCameraMoving: {isCameraMoving}");
                    // Check again in 0.1 seconds
                    var timer = GetTree().CreateTimer(0.1f);
                    timer.Timeout += CheckCameraDone;
                }
            }

            // Start checking after a short delay
            var startTimer = GetTree().CreateTimer(0.1f);
            startTimer.Timeout += CheckCameraDone;
        }
        else
        {
            GD.Print("Camera already at original position, standing immediately");
            PerformStand(targetPosition, duration);
        }
    }
    
    private void PerformStand(Vector3 targetPosition, float duration)
    {
        currentTween?.Kill();

        currentTween = CreateTween();
        currentTween.SetParallel(true);
        
        currentTween.TweenProperty(this, "global_position", targetPosition, duration)
             .SetEase(Tween.EaseType.Out)
             .SetTrans(Tween.TransitionType.Quad);
        
        // Restore original player rotation
        currentTween.TweenProperty(this, "global_transform:basis", _originalPlayerRotation, duration)
             .SetEase(Tween.EaseType.Out)
             .SetTrans(Tween.TransitionType.Quad);
        
        // Also restore camera to its original position relative to player
        currentTween.TweenProperty(MainCamera, "position", _originalCameraPosition, duration)
             .SetEase(Tween.EaseType.Out)
             .SetTrans(Tween.TransitionType.Quad);
        
        currentTween.TweenProperty(MainCamera, "transform:basis", _originalCameraRotation, duration)
             .SetEase(Tween.EaseType.Out)
             .SetTrans(Tween.TransitionType.Quad);

        currentTween.Finished += () =>
        {
            GlobalPosition = targetPosition;
            GlobalTransform = new Transform3D(_originalPlayerRotation, targetPosition);
            
            // Ensure camera is exactly where it should be
            MainCamera.Position = _originalCameraPosition;
            MainCamera.Transform = new Transform3D(_originalCameraRotation, _originalCameraPosition);
            
            sitting = false;
            SetMovementEnabled(true);
            
            GD.Print("Player is now standing");
        };
    }

    private void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
        GD.Print($"Player movement {(enabled ? "enabled" : "disabled")}");
    }
}