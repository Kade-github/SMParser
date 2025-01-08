namespace SMParser;

public class SMFile
{
    private string _filepath;
    // Storage variables for measures
    private int _currentMeasure = 0;
    private List<string> _measure = new List<string>();

    // Storage for difficulty
    private SMDifficulty _workingDiff;

    public SMMetadata metadata;
    public List<SMDifficulty> difficulties;
    public List<SMTimingPoint> timingPoints;

    public SMFile(string filepath)
    {
        _filepath = filepath;
        FileStream _fileStream = new FileStream(filepath, FileMode.Open);
        StreamReader _reader = new StreamReader(_fileStream);
        
        ParseMetadata(_reader);
        ParseDifficulties(_reader);
    }

    private void ParseMeasure()
    {
        float lengthInRows = 192.0f / _measure.Count;
        int rowIndex = 0;
        float beat = 0;
                
        for (int i = 0; i < _measure.Count; i++)
        {
            float noteRow = _currentMeasure * 192 + (lengthInRows * rowIndex);
                    
            beat = noteRow / 48.0f;
                    
            for (int j = 0; j < _measure[i].Length; j++)
            {
                char c = _measure[i][j];
                SMNote note = new SMNote();
                note.beat = beat;
                note.lane = j;
                SMNoteType type = SMNote.ConvertCharToType(c);
                        
                if (type == SMNoteType.Empty)
                    continue;
                        
                note.type = type;
                        
                _workingDiff.notes.Add(note);
            }
                    
            rowIndex++;
        }
                
        _measure.Clear();
        _currentMeasure++;
    }
    
    private void ParseDifficulties(StreamReader _reader)
    {
        difficulties = new List<SMDifficulty>();
        _workingDiff = new SMDifficulty();
        int _index = 0;
        string? line = _reader.ReadLine();
        while (line != null)
        {
            if (line.Length == 0)
            {
                line = _reader.ReadLine();
                continue;
            }

            if (line.Contains("//"))
            {
                line = _reader.ReadLine();
                continue;
            }

            if (line.Contains(":"))
            {
                string value = line.Substring(0, line.IndexOf(":", StringComparison.Ordinal)).Trim();
                if (line.Contains("#NOTES"))
                    _index = 0;
                switch (_index)
                {
                    case 2:
                        _workingDiff.charter = value;
                        break;
                    case 3:
                        _workingDiff.name = value;
                        break;
                }
                _index++;
                
                line = _reader.ReadLine();
                continue;
            }
            
            if (line.Contains(";")) // end of difficulty
            {
                if (_measure.Count > 0)
                    ParseMeasure();
                difficulties.Add(_workingDiff);
                _workingDiff = new SMDifficulty();
                _currentMeasure = 0;
                _measure.Clear();
                line = _reader.ReadLine();
                _index = 0;
                continue;
            }

            if (line.Contains(",")) // parse measure
            {
                ParseMeasure();
            }
            else
            {
                _measure.Add(line);
            }
            
            line = _reader.ReadLine();
        }
        
    }
    
    private void ParseBPMS(string v)
    {
        if (v.Contains(":"))
            v = v.Substring(0, v.IndexOf(":", StringComparison.Ordinal));
        
        if (v.Contains(";"))
            v = v.Substring(0, v.IndexOf(";", StringComparison.Ordinal));

        if (v.Contains(","))
            v = v.Replace(",", "");
        
        string[] bpms = v.Split(',');
        
        foreach (string bpm in bpms)
        {
            string[] bpmData = bpm.Split('=');
            SMTimingPoint tp = new SMTimingPoint();
            tp.bpm = float.Parse(bpmData[1]);
            tp.startBeat = float.Parse(bpmData[0]);
            timingPoints.Add(tp);
        }
    }
    
    private void ParseMetadata(StreamReader _reader)
    {
        metadata = new SMMetadata();
        timingPoints = new List<SMTimingPoint>();
        string? line = _reader.ReadLine();
        int state = 0;
        while (line != null)
        {
            if (line.Length == 0)
            {
                line = _reader.ReadLine();
                continue;
            }

            if (state == 1) // bpms can be newline separated
            {
                if (line.Contains("#"))
                {
                    state = 0;
                    continue;
                }
                if (!line.Contains(";"))
                    state = 0;
                
                ParseBPMS(line);
                line = _reader.ReadLine();
                continue;
            }
            
            if (line.Contains("//"))
                return; // End of metadata

            if (line.StartsWith("#"))
            {
                line = line.Substring(1).Substring(0, line.Length - 2); // Remove # and ;
                
                string key = line.Substring(0, line.IndexOf(":", StringComparison.Ordinal));
                string value = line.Substring(line.IndexOf(":", StringComparison.Ordinal) + 1).Trim();

                switch (key)
                {
                    case "TITLE":
                        metadata.title = value;
                        break;
                    case "TITLETRANSLIT":
                        metadata.titleTranslated = value;
                        break;
                    case "SUBTITLE": 
                        metadata.subtitle = value;
                        break;
                    case "SUBTITLETRANSLIT":
                        metadata.subtitleTranslated = value;
                        break;
                    case "ARTIST":
                        metadata.artist = value;
                        break;
                    case "ARTISTTRANSLIT":
                        metadata.artistTranslated = value;
                        break;
                    case "GENRE":
                        metadata.genre = value;
                        break;
                    case "CREDIT":
                        metadata.credit = value;
                        break;
                    case "BANNER":
                        metadata.banner = value;
                        break;
                    case "BACKGROUND":
                        metadata.background = value;
                        break;
                    case "MUSIC":
                        metadata.music = value;
                        break;
                    case "OFFSET":
                        metadata.offset = float.Parse(value);
                        break;
                    case "SAMPLESTART":
                        metadata.sampleStart = float.Parse(value);
                        break;
                    // Non-Average metadata
                    case "BPMS":
                        state = 1;
                        ParseBPMS(value);
                        break;
                    default:
                        Console.WriteLine("[WARN] " + key + " is not supported right now!");
                        break;
                }
            }
            else
            {
                Console.WriteLine("[WARN] Unexpected line in metadata: " + line);
            }

            line = _reader.ReadLine();
        }
    }
    
    public override string ToString()
    {
        return $"Song: {(metadata.titleTranslated.Length > 0 ? metadata.titleTranslated : metadata.title)} by {(metadata.artistTranslated.Length > 0 ? metadata.artistTranslated : metadata.artist)}. {difficulties.Count} difficulties.";
    }
}