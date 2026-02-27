using Godot;
using System;

public partial class Couch : Node3D, IInteractable
{
    [Export] public Node3D SitPosition; // Position where player should sit
    [Export] public float SitDistance = 1.0f; // How far from couch to place player
    [Export] public float StandUpDistance = 1.5f; // How far to place player when standing
    [Export] public float SitAnimationDuration = 0.8f; // Duration of sitting/standing animation
    [Export] public AudioStream SitSound;
    [Export] public AudioStream StandSound;
    
    private AudioStreamPlayer3D _audioPlayer;
    private bool _isOccupied = false;
    public override void _Ready()
    {
        // Create audio player
        _audioPlayer = new AudioStreamPlayer3D();
        AddChild(_audioPlayer);
        
        // Create sit position if not set in editor
        if (SitPosition == null)
        {
            SitPosition = new Node3D();
            SitPosition.Name = "SitPosition";
            
            // Default sitting position in front of couch
            SitPosition.Position = new Vector3(0, 0, 0.5f);
            SitPosition.Rotation = new Vector3(0, 0, 0);
            
            AddChild(SitPosition);
            GD.Print("Created default SitPosition for couch");
        }
    }

    public void Interact(PlayerCharacter player)
    {
        player.SetText("Interacted");
        if (!_isOccupied)
        {
            SitDown(player);
        }
    }

    private void SitDown(PlayerCharacter player)
    {
        GD.Print("Player sitting on couch");
        
        _isOccupied = true;
        
        // Play sit sound
        PlaySound(SitSound);
        
        // Calculate sitting position
        Vector3 sitWorldPosition = ToGlobal(SitPosition.Position);
        Vector3 sitDirection = -GlobalTransform.Basis.Z; // Forward direction of couch
        
        // Adjust position to be in front of the couch
        Vector3 targetPosition = sitWorldPosition + (sitDirection * SitDistance);
        
        // Face the same direction as the couch (or slightly toward TV if you want)
        Basis targetRotation = GlobalTransform.Basis; // Face same direction as couch
        
        // Alternative: Face toward a specific point (like a TV)
        // Vector3 lookAtPoint = GetNode<Node3D>("../TV").GlobalPosition;
        // Basis targetRotation = Basis.LookingAt(lookAtPoint - targetPosition, Vector3.Up);
        
        // Tell player to sit
        player.SitOnCouch(targetPosition, targetRotation, SitAnimationDuration);
        
        // Optional: Disable player collision while sitting
        // player.SetCollisionLayerValue(1, false);
    }

    private void StandUp(PlayerCharacter player)
    {
        GD.Print("Player standing up from couch");
        
        // Play stand sound
        PlaySound(StandSound);
        
        // Calculate standing position (behind the couch or to the side)
        Vector3 standWorldPosition = ToGlobal(SitPosition.Position);
        Vector3 standDirection = GlobalTransform.Basis.Z; // Behind the couch
        
        Vector3 targetPosition = standWorldPosition + (standDirection * StandUpDistance);
        
        // Tell player to stand
        player.StandFromCouch(targetPosition, SitAnimationDuration);
        
        // Reset couch state
        _isOccupied = false;
        
        // Optional: Re-enable player collision
        // player.SetCollisionLayerValue(1, true);
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
    }

    public void OnUnfocus(PlayerCharacter player)
    {
        player.SetText("");
    }
}