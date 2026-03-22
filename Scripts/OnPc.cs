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

	[Export] public Control Typing;
	[Export] public CharSelect CharSelect;
	[Export] public RichTextLabel Read;
	[Export] public Label Money;
	[Export] public FinalScreen FinalScreen;
	[Export] public Label PriviledgeLabel;
	private ModeEnum Mode { get; set; }

	private Timer fullTimer;
	public int Score = 0;
	public int PotentialScore = 0;
	private int latest = 0;
	public float fullAccuracy = 0;
	public int totalTyped = 0;
	private string wordPath = "res://racist/";
	private string currentWord = "";
	private string typed = "";
	private int ScoreMult = 0;
	private float PercentMult = 1.0f;
	private string CitizenNum = "";
	private bool Priviledge = false;
	private int mult = 8;
	private int minScore = 10;
	private Tween moneyTween;
	[Export] public Label TimerLabel;

	private List<string> easyWordList = new List<string>();
	private List<string> hardWordList = new List<string>();    
	public override void _Ready()
	{
		if(Typing == null) Typing = GetNode<Control>("Typing");
		if(CharSelect == null) CharSelect = GetNode<CharSelect>("CharSelect");
		if(Read == null) Read = Typing.GetNode<VSplitContainer>("Container").GetNode<RichTextLabel>("ReadLabel");
		if(TimerLabel == null) TimerLabel = Typing.GetNode<VSplitContainer>("Container").GetNode<Label>("TimerLabel");
		if(Money == null) Money = Typing.GetNode<Label>("Money");
		if(fullTimer == null) fullTimer = GetNode<Timer>("FullTimer");
		if(FinalScreen == null) FinalScreen = GetNode<FinalScreen>("FinalScreen");
		if(PriviledgeLabel == null) PriviledgeLabel = Typing.GetNode<Label>("PriviledgeLabel");

		Typing.Visible =false;

		GetWords();
		SetWord();
	
	}

	public void OnStart()
	{
		ScoreMult = CharSelect.Score;
		CitizenNum = CharSelect.CitizenNumber;
		Priviledge = CharSelect.IsPriviledged();
		if(Priviledge) {
			PriviledgeLabel.Text = "Priviledged";
			Mode = ModeEnum.easy;
		}
		else {
			PriviledgeLabel.Text = "Not Priviledged";
			Mode = ModeEnum.hard;
		}
		Typing.Visible = true;
		PercentMult = (float)ScoreMult / CharSelect.MAX_SCORE;
		fullTimer.Start();
		
		GD.Print(Mode);
	}
	public override void _Process(double delta)
	{
		TimerLabel.Text = Mathf.Ceil(fullTimer.TimeLeft).ToString();
	}

	private void SetWord()
	{
		if (easyWordList.Count == 0 && hardWordList.Count == 0)
		{
			throw new Exception("No words loaded!");
		}
		var finalPercent =(PercentMult-0.1f) * 0.5f + 0.5f * (Mode == ModeEnum.easy ? 1 
													: Mode == ModeEnum.medium ? 0.7
													: 0.3); 
		if (finalPercent > GD.Randf())
		{
			currentWord = easyWordList[(int)Mathf.Ceil(GD.Randi() % easyWordList.Count)];
			//GD.Print("easy word");
		}
		else
		{
			currentWord = hardWordList[(int)Mathf.Ceil(GD.Randi() % hardWordList.Count)];
			//GD.Print("hard word");
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
		AnimateMoney();
	}

	private void AnimateMoney()
	{
		if (moneyTween != null && moneyTween.IsValid())
		{
			moneyTween.Kill();
			moneyTween = null;
		}
		Money.Visible = true;
		
		Vector2 originalPos = Money.Position;
		Color originalColor = Money.Modulate;
		
		Money.Modulate = Colors.Green;
		
		var tween = CreateTween();
		tween.SetParallel();
		
		tween.TweenProperty(Money, "position", 
			originalPos + new Vector2(0, -20), 1f)
			 .SetEase(Tween.EaseType.Out)
			 .SetTrans(Tween.TransitionType.Quad);
		
		tween.TweenProperty(Money, "modulate", 
			new Color(originalColor, 0f), 1f);
		
		tween.Finished += () => {
			Money.Position = originalPos;
			Money.Modulate = originalColor;
			Money.Visible = false;
		};
		
		Money.SetMeta("money_tween", tween);
	
	}

	private void UpdateColoredWord()
	{
		string result = "";
		string current = currentWord.ToUpper();
		string currTyped = typed.ToUpper();
		for (int i = 0; i < current.Length; i++)
		{
			if (i < currTyped.Length)
			{
				if (currTyped[i] == current[i])
					result += "[color=green]" + current[i] + "[/color]";
				else
					result += "[color=red]" + current[i] + "[/color]";
			}
			else
			{
				result += "[color=white]" + current[i] + "[/color]";
			}
		}

		Read.Text = result;
	}

	private void AddScore()
	{
		totalTyped++;
		fullAccuracy = CalcAccuracy(currentWord, typed);
		var diffMod = Mode == ModeEnum.easy ? 0.75f : 1;
		var greenChars = CorrectChars(currentWord,typed);
		latest = minScore + (int)Mathf.Ceil(greenChars * PercentMult * CalcWordAccuracy(currentWord, typed) * mult * diffMod);
		Score += latest;
		var fullLatest = minScore + (int)Mathf.Ceil(greenChars * CalcWordAccuracy(currentWord, typed) * mult * diffMod);
		PotentialScore += fullLatest;
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
			var wordVar = word.Trim();
			if (!string.IsNullOrEmpty(wordVar))
				easyWordList.Add(wordVar);
		}
		foreach (string word in hardWords.Split('\n'))       
		{
			var wordVar = word.Trim();
			if (!string.IsNullOrEmpty(wordVar))
				hardWordList.Add(wordVar);
		}
	}

	public void Finish()
	{
		Typing.Visible = false;
		fullTimer.Stop();
		FinalScreen.Finish(CitizenNum, Priviledge, Score, fullAccuracy, totalTyped, PotentialScore);
		GD.Print($"{Score} / {PotentialScore}");
	}

	
}
