public class Message
{
    public string? Text { get; set; }
    public string Author { get; set; }
    public DateTime Date { get; set; }
    public bool IsMe { get; set; }
    public string? ImageUrl { get; set; }
}