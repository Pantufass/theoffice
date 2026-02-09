using System;
using System.Collections.Generic;
public class SaveData
{
    public int totalScore = 0;
    public int totalWords = 0;
    public float accuracySum = 0;
    public int plays = 0;

    public List<ScoreData> topTen = new List<ScoreData>();
}