namespace SMParser;

public class SMTimingPoint
{
    public float bpm = 0;
    public float startBeat = 0;
    public float startTime = 0;
    public float endBeat = 0;
    public float endTime = 0;
    
    public override string ToString()
    {
        return $"Timing Point: {bpm} BPM at beat: {startBeat}.";
    }
}