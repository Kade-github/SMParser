namespace SMParser;

public enum SMNoteType {
    Empty = (int)'0',
    Tap = (int)'1',
    Head = (int)'2',
    Tail = (int)'3',
    Mine = (int)'M',
    Fake = (int)'F'
};

public struct SMNote {
    public float beat;
    public int lane;
    public SMNoteType type;
    
    int GetNoteType() {
        switch (type)
        {
            case SMNoteType.Tap:
                return 1;
            case SMNoteType.Head:
                return 2;
            case SMNoteType.Tail:
                return 3;
            case SMNoteType.Mine:
                return 4;
            case SMNoteType.Fake:
                return 5;
        }

        return 0;
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