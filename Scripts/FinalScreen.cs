using Godot;
using System;
using System.Collections.Generic;

public partial class FinalScreen : Control
{
	[Export] public Label FinalScore;
	[Export] public Label TotalScore;
	[Export] public Container HighScoreCont;
	[Export] public Godot.Collections.Array<Container> HighScores;
	private SaveData data;
	private string path = "res://saved/records.json";

	public override void _Ready()
	{
		this.Visible = false;
		if(FinalScore == null) FinalScore = GetNode<Label>("FinalScore");
		if(TotalScore == null) TotalScore = GetNode<Label>("TotalScore");
		if(HighScoreCont == null) HighScoreCont = GetNode<Container>("HighScore");

		if(HighScores == null) HighScores = new Godot.Collections.Array<Container>();

		if(HighScores.Count < 10)
		{
			for(int i = 1; i < 11; i++)
			{
				var name = "Number"+i;
				HighScores.Add(HighScoreCont.GetNode<Container>(name));
			}
		}
		data = FetchData();
	}

	private void ShowScores()
	{
		data.topTen.Sort((a, b) => b.Score.CompareTo(a.Score));
		for (int i = 0; i < Math.Min(10, data.topTen.Count); i++)
		{
			SetLabelText(i,"Name",data.topTen[i].Name);
			SetLabelText(i,"Priviledge",data.topTen[i].Priviledge ? "Priviledged" : "Not Priviledged");
			SetLabelText(i,"Money",data.topTen[i].Score.ToString());
			SetLabelText(i,"NWords",data.topTen[i].Words.ToString());
			SetLabelText(i,"Accuracy",data.topTen[i].Accuracy.ToString());
		}

		TotalScore.Text = $"Total money produced by company: {data.totalScore}$ \n"
						+ $"Total words: {data.totalWords} and Average Accuracy: {data.accuracySum}";
	}

	private void SetLabelText(int index, string label, string text)
	{
		var lavelObj = HighScores[index].GetNode<Label>(label);
		lavelObj.Text = text;
	}

	private SaveData FetchData()
	{
		var fetch = new SaveData();

		if (!FileAccess.FileExists(path))
			return fetch;

		var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		string text = file.GetAsText();

		if (string.IsNullOrWhiteSpace(text))
			return fetch;

		var parsed = Json.ParseString(text);

		if (parsed.VariantType != Variant.Type.Dictionary)
			return fetch;

		var root = parsed.AsGodotDictionary();

		var totals = root["totals"].AsGodotDictionary();
		fetch.totalScore = (int)totals["score"];
		fetch.totalWords = (int)totals["words"];
		fetch.accuracySum = (float)totals["accuracySum"];
		fetch.plays = (int)totals["plays"];

		var arr = root["topTen"].AsGodotArray();
		foreach (var item in arr)
		{
			var d = item.AsGodotDictionary();

			fetch.topTen.Add(new ScoreData
			{
				Name = (string)d["name"],
				Score = (int)d["score"],
				Words = (int)d["words"],
				Accuracy = (float)d["accuracy"]
			});
		}
		return fetch;
	}

	private void SaveToFile(SaveData data)
	{
		try 
		{
			var root = new Godot.Collections.Dictionary();

			var totals = new Godot.Collections.Dictionary
			{
				{ "score", data.totalScore },
				{ "words", data.totalWords },
				{ "accuracySum", data.accuracySum },
				{ "plays", data.plays }
			};

			var topArr = new Godot.Collections.Array();

			foreach (var s in data.topTen)
			{
				topArr.Add(new Godot.Collections.Dictionary
				{
					{ "name", s.Name ?? "" },
					{ "score", s.Score },
					{ "words", s.Words },
					{ "accuracy", s.Accuracy }
				});
			}

			root["totals"] = totals;
			root["topTen"] = topArr;

			// Debug: Check if root has data
			GD.Print($"Saving - totals: {data.totalScore}, {data.totalWords}, top count: {data.topTen.Count}");

			string json = Json.Stringify(root);
			GD.Print($"JSON length: {json?.Length ?? 0}"); // Debug

			if (string.IsNullOrEmpty(json))
			{
				GD.PrintErr("JSON stringify returned empty!");
				return;
			}

			// Ensure directory exists
			var dir = DirAccess.Open("user://");
			if (!dir.DirExists("user://saved"))
			{
				dir.MakeDir("user://saved");
				GD.Print("Created directory: user://saved");
			}

			using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
			if (file == null)
			{
				GD.PrintErr("Failed to open file for writing!");
				return;
			}

			file.StoreString(json);
			GD.Print($"File saved successfully to {path}");
		}
		catch (Exception e)
		{
			GD.PrintErr($"Save error: {e.Message}\n{e.StackTrace}");
		}
	}

	private void SaveScore(ScoreData score, int fullPotential)
	{
		GD.Print($"SaveScore called - score null? {score == null}, fullPotential: {fullPotential}");

		if(score == null)
		{
			GD.Print("Score is null, saving existing data only");
			SaveToFile(data);
			return;
		}

		data.totalScore += fullPotential;
		data.totalWords += score.Words;
		data.accuracySum = ((data.accuracySum * data.plays) + score.Accuracy )/ (data.plays+1);
		data.plays += 1;

		GD.Print($"Updated totals - totalScore: {data.totalScore}, plays: {data.plays}");

		data.topTen.Add(new ScoreData
		{
			Name = score.Name,
			Score = score.Score,
			Words = score.Words,
			Accuracy = score.Accuracy
		});

		GD.Print($"Added to topTen, now count: {data.topTen.Count}");

		data.topTen.Sort((a, b) => b.Score.CompareTo(a.Score));

		if (data.topTen.Count > 10)
		{
			data.topTen = data.topTen.GetRange(0, 10);
			GD.Print("Trimmed to top 10");
		}

		SaveToFile(data);
	}

	public void Finish(string name, bool priviledge, int score, float accuracy, int nWords, int fullPotential)
	{
		this.Visible = true;
		ScoreData finalScore = null;
		if(accuracy < 0.1f)
		{
			FinalScore.Text = "You get 0$ for wasting the company's time.";
		}
		else
		{
			FinalScore.Text = "You earned "+ score
							+"$ with an accuracy of "+accuracy*100
							+"% and a total of "+nWords
							+" words";
			finalScore = new ScoreData(name, priviledge, score, nWords, accuracy);
		}
			
		SaveScore(finalScore, fullPotential);
		ShowScores();
	}

	public void Restart()
	{
		GetTree().ReloadCurrentScene();
	}
}
