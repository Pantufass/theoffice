using Godot;
using System;
using System.Collections.Generic;

public partial class OnPc : CanvasLayer
{
    public enum ModeEnum
    {
        easy,
        medium,
        hard
    }

    [Export] public ColorRect Typing;
    [Export] public CharSelect CharSelect;
    [Export] public Control FinalScreen;
    [Export] public RichTextLabel Read;
    [Export] public Label FinalScore;
    [Export] public Label Money;
    private ModeEnum Mode { get; set; }

    private Timer fullTimer;
    public int Score = 0;
    private int latest = 0;
    public float fullAccuracy = 0;
    public int totalTyped = 0;
    private string wordPath = "res://racist/";
    private string currentWord = "";
    private string typed = "";
    private int ScoreMult = 0;
    private float PercentMult = 1.0f;
    private int mult = 15;
    [Export] public Label TimerLabel;

    private List<string> easyWordList = new List<string>();
    private List<string> hardWordList = new List<string>();    
    public override void _Ready()
    {
        if(Typing == null) Typing = GetNode<ColorRect>("Typing");
        if(CharSelect == null) CharSelect = GetNode<CharSelect>("CharSelect");
        if(Read == null) Read = Typing.GetNode<RichTextLabel>("ReadLabel");
        if(fullTimer == null) fullTimer = GetNode<Timer>("FullTimer");
        if(TimerLabel == null) TimerLabel = GetNode<Label>("TimerLabel");
        if(Money == null) Money = Typing.GetNode<Label>("Money");
        if(FinalScreen == null) FinalScreen = GetNode<Control>("FinalScreen");
        if(FinalScore == null) FinalScore = FinalScreen.GetNode<Label>("FinalScore");

        Typing.Visible =false;
        FinalScreen.Visible = false;
        fullTimer.Start();

        GetWords();
        SetWord();
    
    }

    public void OnStart()
    {
        ScoreMult = CharSelect.Score;
        Mode = ScoreMult >= 8 ? ModeEnum.easy : ModeEnum.hard;
        Typing.Visible = true;
        PercentMult = (float)ScoreMult / CharSelect.MAX_SCORE;
        
        GD.Print(Mode);
    }
    public override void _Process(double delta)
    {
        TimerLabel.Text = Mathf.Ceil(fullTimer.TimeLeft).ToString();
    }

    private void SetWord()
    {
        if (Mode == ModeEnum.easy)
        {
            currentWord = easyWordList[(int)Mathf.Ceil(GD.Randi() % easyWordList.Count)];
        }
        else
        {
            currentWord = hardWordList[(int)Mathf.Ceil(GD.Randi() % hardWordList.Count)];
        }
        typed = "";
        UpdateColoredWord();
    }

    public override void _Input(InputEvent e)
    {
        if(!CharSelect.started) return;
        if (e is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Unicode != 0)
        {
            char c = (char)keyEvent.Unicode;
            typed += c;
            UpdateColoredWord();
        }
        if(typed.Length >= currentWord.Length)
        {
            FinishWord(currentWord, typed);
        }
    }
    public void FinishWord(string word, string typed)
    {
        AddScore();
        SetMoney();
        SetWord();
    }

    public void SetMoney()
    {
        Money.Text = "$" + latest.ToString();
    }

    private void UpdateColoredWord()
    {
        string result = "";

        for (int i = 0; i < currentWord.Length; i++)
        {
            if (i < typed.Length)
            {
                if (typed[i] == currentWord[i])
                    result += "[color=green]" + currentWord[i] + "[/color]";
                else
                    result += "[color=red]" + currentWord[i] + "[/color]";
            }
            else
            {
                result += "[color=white]" + currentWord[i] + "[/color]";
            }
        }

        Read.Text = result;
    }

    private void AddScore()
    {
        totalTyped++;
        fullAccuracy = CalcAccuracy(currentWord, typed);
        var diffMod = Mode == ModeEnum.easy ? 1.2 : 0.9;
        var greenChars = CorrectChars(currentWord,typed);
        latest = (int)Mathf.Ceil(greenChars * PercentMult * CalcWordAccuracy(currentWord, typed) * mult * diffMod);
        Score += latest;
    }

    private float CalcAccuracy(string word, string typed)
    {
        float newWord = CalcWordAccuracy(word, typed);
        return (fullAccuracy * (totalTyped - 1) + newWord) / totalTyped;
    }

    private float CorrectChars(string word, string typed)
    {int correctChars = 0;
        for (int i = 0; i < Math.Min(word.Length, typed.Length); i++)
        {
            if (word[i] == typed[i])
            {
                correctChars++;
            }
        }
        return (float)correctChars;
    }
    private float CalcWordAccuracy(string word, string typed)
    {
        return CorrectChars(word,typed) / word.Length;
    }

    private void GetWords()
    {
        var easypath = ProjectSettings.GlobalizePath(wordPath + "Easywords.txt");
        string easyWords = System.IO.File.ReadAllText(easypath);
        var hardpath = ProjectSettings.GlobalizePath(wordPath + "Hardwords.txt");
        string hardWords = System.IO.File.ReadAllText(hardpath);
        foreach (string word in easyWords.Split('\n'))
        {
            easyWordList.Add(word.Trim());
        }
        foreach (string word in hardWords.Split('\n'))       
        {
            hardWordList.Add(word.Trim());
        }
    }

    public void Finish()
    {
        GD.Print("Final Score: " + Score);
        GD.Print("Final Accuracy: " + fullAccuracy);
        Typing.Visible = false;
        FinalScreen.Visible = true;
        FinalScore.Text = "You earned "+ Score+"$ with an accuracy of "+fullAccuracy*100+"% and a total of "+totalTyped+" words";
    }

    public void Restart()
    {
        GetTree().ReloadCurrentScene();

    }
    
}
