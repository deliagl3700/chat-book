using System.Text;
using System.Text.RegularExpressions;

public class ChatParser
{
    public List<Message> Parse(string path, string myName, string? untilDate = null)
    {
        var lines = File.ReadAllLines(path, Encoding.UTF8);

        var messages = new List<Message>();
        Message? current = null;

        var startRegex = new Regex(@"^\[(.*?)\]\s(.*?):\s(.*)");

        foreach (var line in lines)
        {
            // Detener si se encuentra la fecha especificada
            if (untilDate != null && line.StartsWith($"[{untilDate}"))
            {
                break;
            }

            var match = startRegex.Match(line);

            if (match.Success)
            {
                // Nueva línea de mensaje
                current = new Message
                {
                    Author = match.Groups[2].Value,
                    Text = match.Groups[3].Value,
                    Date = DateTime.Now, // luego parseas si quieres
                    IsMe = match.Groups[2].Value == myName
                };

                messages.Add(current);
            }
            else
            {
                // Continuación del mensaje anterior
                if (current != null)
                {
                    current.Text += "\n" + line;
                }
            }
        }

        return messages;
    }
    public List<Message> Parse2(string path, string myName, string? untilDate = null)
    {
        var lines = File.ReadAllLines(path, Encoding.UTF8);
        var regex = new Regex(@"^\[(.*?)\]\s(.*?):\s([\s\S]*)$");
        var imageRegex = new Regex(@"<attached:\s(.*?)>");

        var messages = new List<Message>();

        foreach (var line in lines)
        {
            // Detener si se encuentra la fecha especificada
            if (untilDate != null && line.StartsWith($"[{untilDate}"))
            {
                break;
            }

            var match = regex.Match(line);
            if (!match.Success) continue;

            var text = match.Groups[3].Value;

            var msg = new Message
            {
                Date = DateTime.Parse(match.Groups[1].Value),
                Author = match.Groups[2].Value,
                IsMe = match.Groups[2].Value == myName
            };

            var imgMatch = imageRegex.Match(text);

            if (imgMatch.Success)
            {
                msg.ImageUrl = $"assets/chat-media/{imgMatch.Groups[1].Value}";
                msg.Text = imageRegex.Replace(text, "").Trim();
            }
            else
            {
                msg.Text = text;
            }

            messages.Add(msg);
        }

        return messages;
    }
}