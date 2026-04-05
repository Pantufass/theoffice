using Godot;
using System;
using System.Collections.Generic;

public partial class Typing : CanvasLayer
{
	[Export] public RichTextLabel TypingLabel;
	private int latest = 0;
	public float fullAccuracy = 0;
	private string words = "The weather outside is fantastic I cant believe we have such a nice day. The sun is shining until so late it feels amazing.";
	private string typed = "";
    private int wrongChars = 0;
    private Tween keyTween;
	[Export] public Label TimerLabel;
    [Export] public Timer fullTimer;
    [Export] public HSplitContainer KeyLine;
    private Dictionary<char, Tween> releaseTweens = new Dictionary<char, Tween>();
    private Dictionary<char, Label> keyMap = new Dictionary<char, Label>();
    public static bool isActive = false;

    [Signal]
    public delegate void TypingCompletedEventHandler(float accuracy);

    [Signal] 
    public delegate void TypingCancelledEventHandler();
    
	public override void _Ready()
	{
		if(TypingLabel == null) TypingLabel = GetNode<RichTextLabel>("Typing/Container/TypingLabel");
		if(TimerLabel == null) TimerLabel = GetNode<Control>("Typing").GetNode<VSplitContainer>("Container").GetNode<Label>("TimerLabel");
        if(fullTimer == null) fullTimer = GetNode<Timer>("Timer");
        if(KeyLine == null) KeyLine = GetNode<HSplitContainer>("%KeyLine");

        foreach (var child in KeyLine.GetChildren())
        {
            if (child is Label label && label.Text.Length == 1)
            {
                char keyChar = label.Text[0];
                keyMap[keyChar] = label;
                label.Modulate = new Color(1, 1, 1, 0);
            }
        }

        TypingLabel.Text = words;

        fullTimer.Start();
	}

	public override void _Process(double delta)
	{
		TimerLabel.Text = Mathf.Ceil(fullTimer.TimeLeft).ToString();
	}
    public void EnableTyping(bool active)
    {
        isActive = active;

        if (active)
        {
            // Reset typing
            typed = "";
            wrongChars = 0;
            TypingLabel.Text = words;
            fullTimer.Start();
            
            // Grab focus on a Control inside this scene
            TypingLabel.GrabFocus();
            
            PlayerInstance.SetTypingMode(true);
            SetProcessInput(true);
        }
        else
        {
            SetProcessInput(false);
            PlayerInstance.SetTypingMode(false);

        }
    }

	public override void _Input(InputEvent e)
	{
        if (!isActive) return;

        if (e is InputEventKey keyEvent)
        {
            if(keyEvent.Pressed && keyEvent.Keycode == Key.Backspace)
            {
                GD.Print("Typing cancelled by user");
                GetViewport().SetInputAsHandled();
                Finished();
                return;
            }

            if (keyEvent.Pressed && keyEvent.Unicode != 0)
            {
                char c = (char)keyEvent.Unicode;
                UpdateColorSentence(c);
                AnimateKey(c);
            }

            if (keyEvent.IsReleased())
            {
                char c = (char)keyEvent.Keycode;
                DisanimateKey(c);
            }
        }

	}

    private void AnimateKey(char c)
    {
        char upperKey = char.ToUpper(c);
        if (keyMap.TryGetValue(upperKey, out Label keyLabel))
        {
            // Kill any pending release tween for this key
            if (releaseTweens.ContainsKey(upperKey) && releaseTweens[upperKey] != null && releaseTweens[upperKey].IsValid())
            {
                releaseTweens[upperKey].Kill();
                releaseTweens.Remove(upperKey);
            }

            // Show key with highlight - make fully visible
            keyLabel.Modulate = Colors.YellowGreen;
            keyLabel.Scale = new Vector2(1.2f, 1.2f);
        }
    }

    public void DisanimateKey(char c)
    {
        char upperKey = char.ToUpper(c);
        if (!keyMap.TryGetValue(upperKey, out Label keyLabel))
            return;

        // Kill any existing tween for this key
        if (releaseTweens.ContainsKey(upperKey) && releaseTweens[upperKey] != null && releaseTweens[upperKey].IsValid())
        {
            releaseTweens[upperKey].Kill();
        }

        // Create new tween
        var tween = CreateTween();
        releaseTweens[upperKey] = tween;

        tween.TweenInterval(0.9f);
        tween.TweenProperty(keyLabel, "modulate:a", 0f, 0.2f);
        tween.TweenProperty(keyLabel, "scale", Vector2.One, 0.2f);

        tween.Finished += () => {
            keyLabel.Modulate = Colors.White with { A = 0 }; 
            keyLabel.Scale = Vector2.One;
            releaseTweens.Remove(upperKey);
        };
    }

	private void UpdateColorSentence(char c)
	{
        if(typed.Length >= words.Length)
        {
            Finished();
            return;
        }

        var colorTyped = "[color=green]" + typed + "[/color]";
        if (c == words[typed.Length])
        {
            typed +=  c;
            colorTyped = "[color=green]" + typed + "[/color]";
        }
        else
        {
            wrongChars++;
            GD.Print("Wrong character: " + c);
        }

        if(typed.Length == words.Length)
        {
            Finished();
        }

		TypingLabel.Text = colorTyped + words.Substring(typed.Length);
	}

    public void Finished()
    {
        fullAccuracy = CalcAccuracy();
        GD.Print("Finished! Accuracy: " + (fullAccuracy * 100).ToString("F2") + "%");
        
        CancelTyping();
    }

    private void CancelTyping()
    {
        fullTimer.Stop();
        EnableTyping(false);
        PlayerInstance.SetTypingMode(false);   
    }

	private float CalcAccuracy()
	{
		return words.Length > 0 ? (words.Length - wrongChars) / (float)words.Length : 0;
	}
	
}
