using System.Text;
using System.Text.RegularExpressions;

public class ChatParser
{
    public List<MessagesByDate> Parse(string path, string myName, string? untilDate = null)
    {
        var lines = File.ReadAllLines(path, Encoding.UTF8);

        var messages = new List<Message>();
        Message? current = null;

        var startRegex = new Regex(@"^\s*\[(.*?)\]\s(.*?):\s(.*)");
        var stickerRegex = new Regex(@"<adjunto:\s(.*?\.webp)>");
        var photoRegex = new Regex(@"<adjunto:\s(.*?\.(jpg|jpeg|png))>");


        foreach (var line in lines)
        {
            // Detener si se encuentra la fecha especificada
            if (untilDate != null && line.StartsWith($"[{untilDate}"))
            {
                break;
            }
            var lineClean = line.TrimStart('\u200E', '\u200F', '\u202A', '\u202C');
            var match = startRegex.Match(lineClean);

            if (match.Success)
            {
                current = new Message
                {
                    Author = match.Groups[2].Value,
                    Text = match.Groups[3].Value.Replace( "<Se editó este mensaje.>",string.Empty),
                    Date = DateTime.Parse(match.Groups[1].Value),
                    IsMe = match.Groups[2].Value == myName
                };

                var matchSticker = stickerRegex.Match(current.Text);
                var matchPhoto = photoRegex.Match(current.Text);
                if (matchPhoto.Success)
                {
                    current.ImageUrl = $"assets/{matchPhoto.Groups[1].Value}";
                    current.Text = null;
                }
                else
                if (matchSticker.Success)
                {
                    current.StickerUrl = $"assets/{matchSticker.Groups[1].Value}";
                    current.Text = null;
                }
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
        var messagesByDate = messages.GroupBy(x=>x.Date.Date).Select(x=> new MessagesByDate { Date = x.Key, messages = x.ToList() }).ToList();
        return messagesByDate;
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