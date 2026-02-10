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
        FetchData();

        data.topTen.Sort((a, b) => b.Score.CompareTo(a.Score));
        for (int i = 0; i < Math.Min(10, data.topTen.Count); i++)
        {
            SetLabelText(i,"Name",data.topTen[i].Name);
            SetLabelText(i,"Money",data.topTen[i].Score.ToString());
            SetLabelText(i,"NWords",data.topTen[i].Words.ToString());
            SetLabelText(i,"Accuracy",data.topTen[i].Accuracy.ToString());
        }
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
        // Totals
        var totals = root["totals"].AsGodotDictionary();
        fetch.totalScore = (int)totals["score"];
        fetch.totalWords = (int)totals["words"];
        fetch.accuracySum = (float)totals["accuracySum"];
        fetch.plays = (int)totals["plays"];

        // Top 10
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
        this.data = fetch;
        return fetch;
    }

    private void SaveToFile(SaveData data)
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
                { "name", s.Name },
                { "score", s.Score },
                { "words", s.Words },
                { "accuracy", s.Accuracy }
            });
        }

        root["totals"] = totals;
        root["topTen"] = topArr;

        var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        file.StoreString(Json.Stringify(root));
    }
    private void SaveScore(ScoreData score)
    {
        var data = FetchData();

        // ---- Update totals ----
        data.totalScore += score.Score;
        data.totalWords += score.Words;
        data.accuracySum += score.Accuracy;
        data.plays += 1;

        // ---- Add to top ten ----
        data.topTen.Add(new ScoreData
        {
            Name = score.Name,
            Score = score.Score,
            Words = score.Words,
            Accuracy = score.Accuracy
        });

        // Sort by score
        data.topTen.Sort((a, b) => b.Score.CompareTo(a.Score));

        // Keep only 10
        if (data.topTen.Count > 10)
            data.topTen = data.topTen.GetRange(0, 10);

        SaveToFile(data);
    }

    public void Finish(string name, int score, float accuracy, int nWords)
    {
        this.Visible = true;
        FinalScore.Text = "You earned "+ score
                        +"$ with an accuracy of "+accuracy*100
                        +"% and a total of "+nWords
                        +" words";
        
        SaveScore(new ScoreData(name, score, nWords, accuracy));
        ShowScores();
    }

    public void Restart()
    {
        GetTree().ReloadCurrentScene();
    }
}
