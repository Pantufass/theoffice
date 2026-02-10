using Godot;
using System;

public partial class CharSelect : ColorRect
{
    public enum RaceEnum
    {
        White,
        Black,
        Asian,
        Hispanic,
        Other
    }  
    public enum GenderEnum
    {
        Male,
        Female,
        NonBinary
    }      
    public enum NativeEnum
    {
        EnglishNative,
        EnglishSpeaker,
        NoSpeaker
    }
    public RaceEnum Race { get; set; }
    public GenderEnum Gender { get; set; }
    public NativeEnum Native { get; set; }
    public int CitizenNumber { get; set; }
    [Export] public Label raceLabel;
    [Export] public Label genderLabel;
    [Export] public Label nativeLabel;
    [Export] public LineEdit citizenLine;

    public static int MAX_SCORE = 20;

    public bool started = false;

    public int Score = 0;
    public override void _Ready()
    {
        base._Ready();
        if(raceLabel == null) raceLabel = GetNode<Label>("%RaceValue");
        if(genderLabel == null) genderLabel = GetNode<Label>("%GenderValue");
        if(nativeLabel == null) nativeLabel = GetNode<Label>("%NativeValue");
        if(citizenLine == null) citizenLine = GetNode<LineEdit>("%CitizenValue");

        raceLabel.Text = RaceEnum.White.ToString();
        genderLabel.Text = GenderEnum.Male.ToString();
        nativeLabel.Text = NativeEnum.EnglishNative.ToString();
        citizenLine.Text = "";
    }

    public void OnRaceNextButtonPressed()
    {
        Race = OnNextButtonPressed<RaceEnum>(Race);
        raceLabel.Text = Race.ToString();
    }
    public void OnGenderNextButtonPressed()
    {
        Gender = OnNextButtonPressed<GenderEnum>(Gender);
        genderLabel.Text = Gender.ToString();
    }
    public void OnNativeNextButtonPressed()
    {        
        Native = OnNextButtonPressed<NativeEnum>(Native);
        nativeLabel.Text = Native.ToString();
    }   

    public void OnRacePreviousButtonPressed()
    {        
        Race = OnPreviousButtonPressed<RaceEnum>(Race);
        raceLabel.Text = Race.ToString();
    }
    public void OnGenderPreviousButtonPressed()
    {   
        Gender = OnPreviousButtonPressed<GenderEnum>(Gender);
        genderLabel.Text = Gender.ToString();
    }
    public void OnNativePreviousButtonPressed()
    {        
        Native = OnPreviousButtonPressed<NativeEnum>(Native);
        nativeLabel.Text = Native.ToString();
    }   

    public T OnNextButtonPressed<T>(T value) where T : Enum
    {
        T[] values = (T[])Enum.GetValues(typeof(T));
        int index = Array.IndexOf(values, value);
        index = (index + 1) % values.Length;
        return values[index];
    }
    
    public T OnPreviousButtonPressed<T>(T value) where T : Enum
    {
        T[] values = (T[])Enum.GetValues(typeof(T));
        int index = Array.IndexOf(values, value);
        index = (index - 1 + values.Length) % values.Length;
        return values[index];
    }
    

    public void OnStart()
    {
        if (int.TryParse(citizenLine.Text, out int citizenNumber))
        {
            CitizenNumber = citizenNumber;
        }
        GetScore();
        this.Visible = false;
        started = true;
        GetParent<OnPc>().OnStart();
    }

    private int CalcCitizenNumber()
    {
        return CitizenNumber % 2 == 0 ? CalcCitizen() : 0;
    }
    private int CalcCitizen()
    {
        if(CitizenNumber >= 100000 && CitizenNumber <= 1000000) return 3;
        if(CitizenNumber >= 100000000 && CitizenNumber <= 1000000000) return 3;
        return 0;
    }
    private int CalcRace()
    {
        if(Race == RaceEnum.Black) return 0;
        if(Race == RaceEnum.Other) return 1;
        if(Race == RaceEnum.Hispanic) return 2;
        if(Race == RaceEnum.Asian) return 3;
        if(Race == RaceEnum.White) return 5;
        return 0;
    }
    private int CalcGender()
    {
        if(Gender == GenderEnum.Female) return 2;
        if(Gender == GenderEnum.NonBinary) return 0;
        if(Gender == GenderEnum.Male) return 4;
        return 0;
    }
    private int CalcNative()
    {
        if(Native == NativeEnum.EnglishNative) return 2;
        if(Native == NativeEnum.EnglishSpeaker) return 1;
        if(Native == NativeEnum.NoSpeaker) return 0;
        return 0;
    }
    public void GetScore()
    {
        Score = 6;  
        Score += CalcCitizen();
        Score += CalcRace();
        Score += CalcGender();
        Score += CalcNative();
        if(Score > MAX_SCORE) Score = MAX_SCORE;
    }

}
