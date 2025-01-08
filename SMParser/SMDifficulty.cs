namespace SMParser;

public enum SMNoteType {
    Empty,
    Tap,
    Head,
    Tail,
    Mine,
    Fake
};

public struct SMNote {
    public float beat;
    public int lane;
    public SMNoteType type;

    public static SMNoteType ConvertCharToType(char c)
    {
        switch (c)
        {
            case '1':
                return SMNoteType.Tap;
            case '2':
                return SMNoteType.Head;
            case '3':
                return SMNoteType.Tail;
            case 'M':
                return SMNoteType.Mine;
            case 'F':
                return SMNoteType.Fake;
        }
        return SMNoteType.Empty;
    }
    
    public override string ToString()
    {
        return $"Note: {type} at {beat} beat on lane {lane}.";
    }
};

public class SMDifficulty
{
    public string name = "";
    public string charter = "";

    public List<SMNote> notes = new List<SMNote>();
    
    public override string ToString()
    {
        return $"Difficulty: {name} by {charter}. {notes.Count} notes.";
    }
}