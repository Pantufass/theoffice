using Godot;
using System;

public partial class Door : MeshInstance3D, IInteractable
{
    [Export] public float ClosedAngle = -90f; // Degrees to rotate when closing
    [Export] public AudioStream CloseAndLockSound;
    [Export] public AudioStream DeniedSound;
    [Export] public Area3D OutsideArea;
    
    private bool _isClosedAndLocked = false;
    private bool _playerOutside = false;
    private Tween _currentTween;
    private AudioStreamPlayer3D _audioPlayer;
    
    private float _openAngle;
    private float _targetAngle;

    public bool Active = false;

    public override void _Ready()
    {
        _audioPlayer = new AudioStreamPlayer3D();
        AddChild(_audioPlayer);
        
        SetupOutsideArea();
        
        if (OutsideArea != null)
        {
            OutsideArea.BodyEntered += OnOutsideAreaEntered;
            OutsideArea.BodyExited += OnOutsideAreaExited;
        }
        
        // Store current rotation as open angle
        _openAngle = RotationDegrees.Y;
        _targetAngle = _openAngle + ClosedAngle;
        
        GD.Print($"Door ready - open at {_openAngle}°, will close to {_targetAngle}°");
    }
    
    private void SetupOutsideArea()
    {
        if (OutsideArea == null)
        {
            OutsideArea = new Area3D();
            OutsideArea.Name = "OutsideArea";
            
            CollisionShape3D shape = new CollisionShape3D();
            BoxShape3D boxShape = new BoxShape3D();
            boxShape.Size = new Vector3(2.0f, 2.5f, 1.5f);
            shape.Shape = boxShape;
            OutsideArea.AddChild(shape);
            
            OutsideArea.Position = new Vector3(0, 1.25f, -1.5f);
            AddChild(OutsideArea);
        }
    }
    
    private void OnOutsideAreaEntered(Node body)
    {
        if (body is PlayerCharacter)
            _playerOutside = true;
    }
    
    private void OnOutsideAreaExited(Node body)
    {
        if (body is PlayerCharacter)
            _playerOutside = false;
    }

    public void Interact(PlayerCharacter player)
    {
        if(!Active) return;
        if (_isClosedAndLocked)
        {
            player.SetText("Door is locked");
            PlaySound(DeniedSound);
            return;
        }
        
        if (!_playerOutside)
        {
            player.SetText("Must be in corridor");
            PlaySound(DeniedSound);
            return;
        }
        
        CloseAndLock();
    }

    private void CloseAndLock()
    {
        GD.Print("Closing and locking door");
        
        _currentTween?.Kill();
        
        PlaySound(CloseAndLockSound);
        
        _currentTween = CreateTween();
        _currentTween.TweenProperty(this, "rotation_degrees:y", _targetAngle, 0.8f)
             .SetEase(Tween.EaseType.Out)
             .SetTrans(Tween.TransitionType.Quad);
        
        _currentTween.Finished += () =>
        {
            RotationDegrees = new Vector3(0, _targetAngle, 0);
            _isClosedAndLocked = true;
            
            GD.Print("Door is now closed and locked");
            
            if (OutsideArea != null)
                OutsideArea.Monitoring = false;
            
        };
    }
    
    private void PlaySound(AudioStream sound)
    {
        if (sound != null && _audioPlayer != null)
        {
            _audioPlayer.Stream = sound;
            _audioPlayer.Play();
        }
    }

    public void OnFocus(PlayerCharacter player)
    {
        if(!Active) return;
        if (_isClosedAndLocked)
            player.SetText("Door is locked");
        
    }

    public void OnUnfocus(PlayerCharacter player)
    {
        if(!Active) return;
        player.SetText("");
    }
}