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

    public void Save(string path = "")
    {
        string fp = path.Length > 0 ? path : this._filepath;

        using FileStream fileStream = new FileStream(fp, FileMode.Create);
        StreamWriter writer = new StreamWriter(fileStream);
        // Write metadata
        writer.WriteLine("#TITLE:{0};", metadata.title);
        writer.WriteLine("#SUBTITLE:{0};", metadata.subtitle);
        writer.WriteLine("#ARTIST:{0};", metadata.artist);
        writer.WriteLine("#TITLETRANSLIT:{0};", metadata.titleTranslated);
        writer.WriteLine("#SUBTITLETRANSLIT:{0};", metadata.subtitleTranslated);
        writer.WriteLine("#ARTISTTRANSLIT:{0};", metadata.artistTranslated);
        writer.WriteLine("#GENRE:{0};", metadata.genre);
        writer.WriteLine("#CREDIT:{0};", metadata.credit);
        writer.WriteLine("#BANNER:{0};", metadata.banner);
        writer.WriteLine("#BACKGROUND:{0};", metadata.background);
        writer.WriteLine("#MUSIC:{0};", metadata.music);
        writer.WriteLine("#OFFSET:{0};", metadata.offset);
        writer.WriteLine("#SAMPLESTART:{0};", metadata.sampleStart);
        // Write BPMs
        writer.Write("#BPMS:");
        for (int i = 0; i < timingPoints.Count; i++)
        {
            writer.Write("{0}={1}", timingPoints[i].startBeat, timingPoints[i].bpm);
            if (i < timingPoints.Count - 1)
                writer.Write(",");
            else 
                writer.Write(";");
        }
        writer.WriteLine();
        writer.WriteLine("#STOPS:;");
        // Write difficulties
        foreach (var diff in difficulties)
        {
            writer.WriteLine($"//--------------- {diff.type} - {diff.charter} ----------------");
            writer.WriteLine("#NOTES:");
            writer.WriteLine("     {0}:", diff.type);
            writer.WriteLine("     {0}:", diff.charter);
            writer.WriteLine("     {0}:", diff.name);
            writer.WriteLine("     1:");
            writer.WriteLine("     0,0,0,0,0:");
            // Write measures
            float lastBeat = diff.notes.Last().beat;
            float lastMeasure = (lastBeat / 4.0f);

            for (int i = 0; i <= lastMeasure; i++)
            {
                int startRow = i * 192;
                int lastRow = (i + 1) * 192 - 1;
                List<SMNote> measureLines = new List<SMNote>();
                
                float startBeat = startRow / 48.0f;
                
                foreach (var note in diff.notes)
                {
                    if (note.beat >= startBeat && note.beat < startBeat + 4)
                        measureLines.Add(note);
                }
                
                int rowSpacing = (int)Math.Round(GetSmallestSnapInMeasure(measureLines) * 48.0f);
                for (int row = startRow; row <= lastRow; row += rowSpacing)
                {
                    float rowBeat = row / 48.0f;
                    System.Collections.Generic.List<string> directions = new System.Collections.Generic.List<string> { "0", "0", "0", "0"};
                    if (diff.type.Contains("double"))
                        directions = new System.Collections.Generic.List<string> { "0", "0", "0", "0", 
                            "0", "0", "0", "0"};
                    
                    foreach (var note in measureLines)
                    {
                        // ReSharper disable once CompareOfFloatsByEqualityOperator
                        if (note.beat == rowBeat)
                        {
                            directions[note.lane] = note.type switch
                            {
                                SMNoteType.Tap => "1",
                                SMNoteType.Head => "2",
                                SMNoteType.Tail => "3",
                                SMNoteType.Mine => "M",
                                SMNoteType.Fake => "F",
                                _ => "0"
                            };
                            
                        }
                    }
                    
                    foreach (var dir in directions)
                        writer.Write(dir);
                    
                    writer.WriteLine(); // New line after each row
                }
                
                if (i < lastMeasure)
                    writer.WriteLine(","); // New measure
            }
            
            writer.WriteLine(";"); // End of difficulty
        }
        
        writer.Flush();
        writer.Close();
        fileStream.Close();
    }

    private float GetSmallestSnapInMeasure(List<SMNote> notes)
    {
        float smallestSnap = 1.0f;

        List<float> snaps = new List<float>();
        
        foreach (var note in notes)
        {
            float beatRow = note.beat * 48.0f;
            float snap = 0.0f;

            if (beatRow % 48 == 0) // 4th
                snap = 1.0f;
            else if (beatRow % 24 == 0) // 8th
                snap = 2.0f;
            else if (beatRow % 16 == 0) // 12th
                snap = 3.0f;
            else if (beatRow % 12 == 0) // 16th
                snap = 4.0f;
            else if (beatRow % 8 == 0) // 24th
                snap = 6.0f;
            else if (beatRow % 6 == 0) // 32nd
                snap = 8.0f;
            else if (beatRow % 4 == 0) // 48th
                snap = 12.0f;
            else if (beatRow % 3 == 0) // 64th
                snap = 16.0f;
            else if (beatRow % 2 == 0) // 96th
                snap = 24.0f;
            else // 192nd
                snap = 48.0f;
            
            snaps.Add(snap);
            
            if (snap > smallestSnap)
                smallestSnap = snap;
        }
        
        // If the snap contains a 12th, but it also contains a bigger snap; force it to be a 48th so it contains 24th, 16th, 12th, 8th, and 4th.
        if ((snaps.Contains(3.0f) || snaps.Contains(6.0f)) && smallestSnap < 8.0f)
            smallestSnap = 12.0f;
        
        // If the snap contains a 32nd, and it contains either a 12th, 24th, or 48th; force it to 192nd.
        if (snaps.Contains(8.0f) && (snaps.Contains(3.0f) || snaps.Contains(6.0f) || snaps.Contains(12.0f)))
            smallestSnap = 48.0f;
        
        return 1.0f / smallestSnap;
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
                    case 1:
                        _workingDiff.type = value;
                        break;
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
                if (line.Contains("#") || line.Contains(";"))
                {
                    state = 0;
                    continue;
                }
                
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
                        metadata.offset = (float)double.Parse(value);
                        break;
                    case "SAMPLESTART":
                        metadata.sampleStart = (float)double.Parse(value);
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