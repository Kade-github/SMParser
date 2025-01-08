namespace SMParser;

public class SMMetadata
{
    public string artist = "";
    public string artistTranslated = "";
    public string title = "";
    public string titleTranslated = "";
    public string subtitle = "";
    public string subtitleTranslated = "";
    
    public string genre = "";
    public string credit = "";
    
    public string banner = "";
    public string background = "";
    
    public string music = "";
    
    public float offset = 0;
    public float sampleStart = 0;
    
    public override string ToString()
    {
        return $"Metadata: {title} by {artist}. {subtitle} - {genre}.";
    }
}