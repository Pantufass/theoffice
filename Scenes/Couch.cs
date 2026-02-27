using Godot;
using System;

public partial class Couch : Node3D, IInteractable
{
    [Export] public Node3D SitPosition;
    [Export] public float SitDistance = 1.0f;
    [Export] public float StandUpDistance = 1.5f;
    [Export] public float SitAnimationDuration = 0.8f;
    [Export] public AudioStream SitSound;
    [Export] public AudioStream StandSound;
    [Export] public Node3D TVTarget;
    [Export] public bool AutoZoomToTV = true;
    [Export] public float CameraZoomDelay = 0.3f; // Delay after sitting before zoom
    
    private AudioStreamPlayer3D _audioPlayer;
    private bool _isOccupied = false;
    private PlayerCharacter _currentPlayer;

    public override void _Ready()
    {
        _audioPlayer = new AudioStreamPlayer3D();
        AddChild(_audioPlayer);
        
        if (SitPosition == null)
        {
            SitPosition = new Node3D();
            SitPosition.Name = "SitPosition";
            SitPosition.Position = new Vector3(0, 0, 0.5f);
            AddChild(SitPosition);
        }
        
        if (TVTarget == null)
        {
            TVTarget = GetNodeOrNull<Node3D>("../TV");
        }
    }

    public void Interact(PlayerCharacter player)
    {
        if (!_isOccupied)
        {
            SitDown(player);
        }
        else if (_isOccupied && _currentPlayer == player)
        {
            StandUp(player);
        }
    }

    private void SitDown(PlayerCharacter player)
    {
        GD.Print("Player sitting on couch");
        
        _isOccupied = true;
        _currentPlayer = player;
        
        PlaySound(SitSound);
        
        // Calculate sitting position
        Vector3 sitWorldPosition = ToGlobal(SitPosition.Position);
        Vector3 sitDirection = -GlobalTransform.Basis.Z;
        Vector3 targetPosition = sitWorldPosition + (sitDirection * SitDistance);
        
        // Face player toward TV
        Basis targetRotation;
        if (TVTarget != null)
        {
            Vector3 directionToTV = (TVTarget.GlobalPosition - targetPosition).Normalized();
            targetRotation = Basis.LookingAt(directionToTV, Vector3.Up);
        }
        else
        {
            targetRotation = GlobalTransform.Basis;
        }
        
        // Sit the player
        player.SitOnCouch(targetPosition, targetRotation, SitAnimationDuration);
        
        // Zoom camera to TV after sitting
        if (AutoZoomToTV && TVTarget != null)
        {
            // Use a timer to zoom after sitting animation completes
            var timer = GetTree().CreateTimer(SitAnimationDuration + CameraZoomDelay);
            timer.Timeout += () => {
                if (_isOccupied && _currentPlayer == player)
                {
                    GD.Print("Zooming camera to TV");
                    player.ZoomToTV(TVTarget.GlobalTransform);
                }
            };
        }
    }

    private void StandUp(PlayerCharacter player)
    {
        GD.Print("Player standing up from couch");
        
        // Return camera first
        player.ReturnCamera();
        
        PlaySound(StandSound);
        
        // Calculate standing position
        Vector3 standWorldPosition = ToGlobal(SitPosition.Position);
        Vector3 standDirection = GlobalTransform.Basis.Z;
        Vector3 targetPosition = standWorldPosition + (standDirection * StandUpDistance);
        
        // Small delay to let camera return before standing
        var timer = GetTree().CreateTimer(0.3f);
        timer.Timeout += () => {
            if (!_isOccupied) return; // Check if we haven't been re-interacted
            player.StandFromCouch(targetPosition, SitAnimationDuration);
            _isOccupied = false;
            _currentPlayer = null;
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
        if (!_isOccupied)
        {
            player.SetText("Sit");
        }
        else if (_isOccupied && _currentPlayer == player)
        {
            player.SetText("Stand Up");
        }
    }

    public void OnUnfocus(PlayerCharacter player)
    {
        player.SetText("");
    }
}