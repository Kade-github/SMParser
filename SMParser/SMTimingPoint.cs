namespace SMParser;

public class SMTimingPoint
{
    public float bpm = 0;
    public float startBeat = 0;
    
    public override string ToString()
    {
        return $"Timing Point: {bpm} BPM at beat: {startBeat}.";
    }
}