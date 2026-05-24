public class MessagesByDate
{
    public DateTime Date { get; set; }
    public List<Message> messages { get; set; } = new List<Message>();  
}

public class Message
{
    public string? Text { get; set; }
    public string Author { get; set; }
    public DateTime Date { get; set; }
    public bool IsMe { get; set; }
    public string? StickerUrl { get; set; }
    public string? ImageUrl { get; set; }
    public string? AudioUrl { get; set; }
    public string? VideoUrl { get; set; }

    public string? QrCode { get; set; } 
}