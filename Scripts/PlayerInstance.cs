using Godot;

public static class PlayerInstance
{
    public static PlayerCharacter Player { get; set; }
    public static bool IsTypingActive { get; private set; }
    public static bool IsMovementEnabled { get; set; } = true;
    
    public static void SetTypingMode(bool active)
    {
        IsTypingActive = active;
        
        if (active)
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            if (Player != null)
            {
                Player.Enabled = false;
                Player.SetProcessInput(false);
                Player.SetPhysicsProcess(false);
            }
        }
        else
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            
            if (Player != null)
            {
                Player.Enabled = true;
                Player.SetProcessInput(true);
                Player.SetPhysicsProcess(true);
                
                // Force reset camera rotation handling
                Player.CallDeferred("_Input", new InputEventMouseMotion()); // Triggers camera reset if needed
            }
            
            Callable.From(() => {
                if (Input.MouseMode != Input.MouseModeEnum.Captured)
                {
                    Input.MouseMode = Input.MouseModeEnum.Captured;
                    GD.Print("Mouse recaptured via delay");
                }
            }).CallDeferred();
        }
    }
}