public class MessagesByDate
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get
        {
            return new DateTime(Year, Month, 1).ToString("MMMM");
        }
    }
    public List<DayGroup> dayGroup { get;set;} = new List<DayGroup>();
}

public class DayGroup
{
    public DateTime Date { get; set; }
    public List<Message> Messages { get; set; } = new List<Message>();

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