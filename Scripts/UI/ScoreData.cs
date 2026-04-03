public class ScoreData
{
	public ScoreData(){}
	public ScoreData(string name, bool priviledge, int score, int nWords, float accuracy)
	{
		Name = name;
		Priviledge = priviledge;
		Score = score;
		Words = nWords;
		Accuracy = accuracy;
	}

	public string Name { get; set; }
	public bool Priviledge { get; set; }
	public int Score { get; set; }
	public int Words { get; set; }
	public float Accuracy { get; set; }
}
