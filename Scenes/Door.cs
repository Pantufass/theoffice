using Godot;
using System;

public partial class Door : MeshInstance3D, IInteractable
{
    [Export] public float OpenAngle = 90f;
    [Export] public AudioStream OpenSound;
    [Export] public AudioStream CloseSound;
    [Export] public bool IsLocked = false;
    
    private bool _isOpen = true;
    private Tween _currentTween;
    private AudioStreamPlayer3D _audioPlayer;
    private float _closedAngle;
    private float _openAngle;

    public override void _Ready()
    {
        _audioPlayer = new AudioStreamPlayer3D();
        AddChild(_audioPlayer);
        
        // Store angles
        _closedAngle = RotationDegrees.Y;
        _openAngle = _closedAngle + OpenAngle;
        
        // Start open
        RotationDegrees = new Vector3(0, _openAngle, 0);
    }

    public void Interact(PlayerCharacter player)
    {
        if (IsLocked)
        {
            GD.Print("Door is locked!");
            return;
        }
        
        _currentTween?.Kill();
        _currentTween = CreateTween();
        _currentTween.SetEase(Tween.EaseType.InOut);
        _currentTween.SetTrans(Tween.TransitionType.Sine);
        
        float targetAngle = _isOpen ? _closedAngle : _openAngle;
        
        // Play appropriate sound
        AudioStream soundToPlay = _isOpen ? CloseSound : OpenSound;
        if (soundToPlay != null)
        {
            _audioPlayer.Stream = soundToPlay;
            _audioPlayer.Play();
        }
        
        // Animate rotation only
        _currentTween.TweenProperty(this, "rotation_degrees:y", targetAngle, 0.5f);
        
        _isOpen = !_isOpen;
    }

    public void OnFocus()
    {
        string prompt = IsLocked ? "Locked" : (_isOpen ? "Close" : "Open");
        GD.Print($"Press E to {prompt} door");
        Scale = new Vector3(1.05f, 1.05f, 1.05f);
    }

    public void OnUnfocus()
    {
        Scale = Vector3.One;
    }
}