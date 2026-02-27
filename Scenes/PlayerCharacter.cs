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

	private const float NORMAL_SPEED = 1f;
	[Export] public float SprintSpeed = 1.5f;
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
    private Transform3D originalCameraTransform;
    private Transform3D targetTVTransform;
    [Export] public Node3D TV;
    private bool sitting = false;
    public bool Sitting {get => sitting;}
    private Tween currentTween;    
    private bool isCameraMoving = false;
    private Vector3 _originalPosition;
    private Basis _originalRotation;
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
        originalCameraTransform = MainCamera.Transform;
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
        if(Input.IsActionJustPressed("up") || Input.IsActionJustPressed("down") ||
            Input.IsActionJustPressed("right") || Input.IsActionJustPressed("right") )
        {
            ReturnCamera();
        }
        if(Input.IsActionJustPressed("sit"))
        {
            var tv = GetNode<TV>("../%TV");
            if(tv != null) GD.Print("TV found");
            ZoomToTV(TV.Transform);
        }
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
		if (input is InputEventMouseMotion motionEvent)
		{
			Vector2 mouseMovement = motionEvent.Relative * mouseSensitivity;
			CameraLook(mouseMovement);
		}

		// ───── Crouch ─────
		if (EnableCrouch)
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
		if (EnableSprint)
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
				CurrentInteractable = found;
				CurrentInteractable.OnFocus(this);
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
        GD.Print("=== Zoom To TV ===");
        GD.Print($"TV Transform - Origin: {tvTransform.Origin}, Basis: {tvTransform.Basis}");
        GD.Print($"Camera Current - Origin: {MainCamera.GlobalPosition}");
        
        if (isCameraMoving) return;
        
        // Kill any existing tween
        currentTween?.Kill();
        
        // Store original camera position - MAKE A DEEP COPY
        originalCameraTransform = new Transform3D(
            MainCamera.GlobalTransform.Basis,
            MainCamera.GlobalTransform.Origin
        );
        
        // Calculate target position and transform
        float zoomDistance = 2.0f;
        Vector3 tvForward = -tvTransform.Basis.Z; // TV's forward direction
        Vector3 tvUp = tvTransform.Basis.Y; // TV's up direction
        
        // Position camera directly in front of TV
        Vector3 targetPosition = tvTransform.Origin + (tvForward * zoomDistance);
        
        // Create transform that looks at the TV
        targetTVTransform = new Transform3D();
        targetTVTransform.Origin = targetPosition;
        targetTVTransform.Basis = Basis.LookingAt(tvTransform.Origin - targetPosition, tvUp);
        
        GD.Print($"Target - Origin: {targetPosition}");
        GD.Print($"Target - Looking at: {tvTransform.Origin - targetPosition}");
        
        // Disable player input
        SetPlayerInputEnabled(false);
        isCameraMoving = true;
        
        // Create tween
        currentTween = CreateTween();
        currentTween.SetParallel(false); // Run sequentially for more control
        
        // First tween position
        currentTween.TweenProperty(MainCamera, "global_position", targetPosition, 0.8f)
             .SetEase(Tween.EaseType.Out)
             .SetTrans(Tween.TransitionType.Quad);
        
        // Then tween rotation (or use parallel with both)
        // For parallel:
        // currentTween.SetParallel(true);
        // currentTween.TweenProperty(MainCamera, "global_position", targetPosition, 1.0f)
        //      .SetEase(Tween.EaseType.Out)
        //      .SetTrans(Tween.TransitionType.Quad);
        // currentTween.TweenProperty(MainCamera, "global_transform:basis", targetTVTransform.Basis, 1.0f)
        //      .SetEase(Tween.EaseType.Out)
        //      .SetTrans(Tween.TransitionType.Quad);
        
        // Set up completion callback
        currentTween.Finished += OnZoomToTVComplete;
    }
    
    private void OnZoomToTVComplete()
    {
        // Force exact position and rotation
        MainCamera.GlobalPosition = targetTVTransform.Origin;
        MainCamera.GlobalTransform = new Transform3D(targetTVTransform.Basis, targetTVTransform.Origin);
        
        sitting = true;
        isCameraMoving = false;
        
        GD.Print($"Camera reached TV - Position: {MainCamera.GlobalPosition}");
        GD.Print($"Expected Position: {targetTVTransform.Origin}");
    }
    
    internal void ReturnCamera()
    {
        GD.Print("=== Return Camera ===");
        GD.Print($"Current Camera: {MainCamera.GlobalPosition}");
        GD.Print($"Original Camera: {originalCameraTransform.Origin}");
        
        if (!sitting || isCameraMoving) return;
        
        isCameraMoving = true;
        
        currentTween?.Kill();
        
        currentTween = CreateTween();
        
        // Make a fresh copy of the original transform
        Transform3D returnTransform = new Transform3D(
            originalCameraTransform.Basis,
            originalCameraTransform.Origin
        );
        
        GD.Print($"Returning to: {returnTransform.Origin}");
        
        // Tween back - use sequential for better control
        currentTween.TweenProperty(MainCamera, "global_position", returnTransform.Origin, 0.8f)
             .SetEase(Tween.EaseType.Out)
             .SetTrans(Tween.TransitionType.Quad);
        
        // Set up completion callback
        currentTween.Finished += () => OnReturnCameraComplete(returnTransform);
    }
    
    private void OnReturnCameraComplete(Transform3D expectedTransform)
    {
        // Force exact original position and rotation
        MainCamera.GlobalPosition = expectedTransform.Origin;
        MainCamera.GlobalTransform = new Transform3D(expectedTransform.Basis, expectedTransform.Origin);
        
        sitting = false;
        isCameraMoving = false;
        
        // Re-enable player input
        SetPlayerInputEnabled(true);
        
        GD.Print($"Camera returned - Position: {MainCamera.GlobalPosition}");
        GD.Print($"Expected Position: {expectedTransform.Origin}");
    }

    private void SetPlayerInputEnabled(bool enabled)
    {
        var playerController = GetNodeOrNull<CharacterBody3D>(".."); 

        if (playerController != null)
        {
            if (playerController.HasMethod("SetMovementEnabled"))
            {
                playerController.Call("SetMovementEnabled", enabled);
            }
        }

        GD.Print($"Player input {(enabled ? "enabled" : "disabled")}");
    }

    public void SitOnCouch(Vector3 targetPosition, Basis targetRotation, float duration)
    {
        if (sitting) return;

        GD.Print($"Player sitting at position: {targetPosition}");

        // Store original transform
        _originalPosition = GlobalPosition;
        _originalRotation = GlobalTransform.Basis;

        // Kill any existing tween
        currentTween?.Kill();

        // Disable movement
        SetMovementEnabled(false);

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
            sitting = true;
            GD.Print("Player is now sitting");
        };
    }

    public void StandFromCouch(Vector3 targetPosition, float duration)
    {
        if (!sitting) return;

        GD.Print($"Player standing at position: {targetPosition}");

        currentTween?.Kill();

        currentTween = CreateTween();
        currentTween.SetParallel(true);
        currentTween.TweenProperty(this, "global_position", targetPosition, duration)
             .SetEase(Tween.EaseType.Out)
             .SetTrans(Tween.TransitionType.Quad);

        // Optional: Add a small upward motion for standing
        // Vector3 standWithBounce = targetPosition + new Vector3(0, 0.2f, 0);
        // _currentTween.TweenProperty(this, "global_position", standWithBounce, duration * 0.5f)
        //      .SetEase(Tween.EaseType.Out);

        currentTween.Finished += () =>
        {
            GlobalPosition = targetPosition;
            sitting = false;

            // Re-enable movement
            SetMovementEnabled(true);

            GD.Print("Player is now standing");
        };
    }

    private void SetMovementEnabled(bool enabled)
    {
        // Implement based on your movement system
        // For CharacterBody3D:
        // SetPhysicsProcess(enabled);
        // SetProcess(enabled);

        // Or if you have a custom flag:
        movementEnabled = enabled;

        GD.Print($"Player movement {(enabled ? "enabled" : "disabled")}");
    }

}
