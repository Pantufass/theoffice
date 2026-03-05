public class ScoreData
{
	public ScoreData(){}
	public ScoreData(string name, int score, int nWords, float accuracy)
	{
		Name = name;
		Score = score;
		Words = nWords;
		Accuracy = accuracy;
	}

	public string Name { get; set; }
	public int Score { get; set; }
	public int Words { get; set; }
	public float Accuracy { get; set; }
}
