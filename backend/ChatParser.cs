using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using QRCoder;
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
        var audioRegex = new Regex(@"<adjunto:\s(.*?\.opus)>");
        var videoRegex = new Regex(@"<adjunto:\s(.*?\.mp4)>");
        var json = File.ReadAllText("data/media-map.json");

        var mediaList = JsonSerializer.Deserialize<List<MediaItem>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

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
                var matchAudio = audioRegex.Match(current.Text);
                var matchVideo = videoRegex.Match(current.Text);

                if (matchPhoto.Success)
                {
                    current = TreatImageData(messages, current, photoRegex, matchPhoto);
                }
                else if (matchSticker.Success)
                {
                    current.StickerUrl = $"assets/{matchSticker.Groups[1].Value}";
                    current.Text = null;
                } else if (matchAudio.Success)
                {
                    current.AudioUrl = $"{matchAudio.Groups[1].Value}";
                    var url =  mediaList?.Find(x=>x.FileName == current.AudioUrl)?.Url ?? string.Empty;
                    current.AudioUrl = url;
                    current.QrCode = GenerateQrBase64(url);
                    current.Text = null;
                }else if (matchVideo.Success)
                {
                    current.VideoUrl = $"{matchVideo.Groups[1].Value}";
                    var url =  mediaList?.Find(x=>x.FileName == current.VideoUrl)?.Url ?? string.Empty;
                    current.VideoUrl = url;
                    current.QrCode = GenerateQrBase64(url);
                    current.Text = null;
                }
                if (current != null)
                {
                    messages.Add(current);
                }
            }
            else
            {
                // Continuación del mensaje anterior
                current?.Text += "\n" + line;
            }
        }
        var messagesByDate = messages.GroupBy(x=>x.Date.Date).Select(x=> new MessagesByDate { Date = x.Key, messages = x.ToList() }).ToList();
        return messagesByDate;
    }

    private static Message? TreatImageData(List<Message> messages, Message? current, Regex photoRegex, Match matchPhoto)
    {
        var imagePath = $"assets/{matchPhoto.Groups[1].Value}";

        // Eliminar solo la etiqueta de adjunto y obtener el texto restante
        var remainingText = photoRegex.Replace(current.Text ?? string.Empty, string.Empty).Trim();

        if (!string.IsNullOrWhiteSpace(remainingText))
        {

            var imgMsg = new Message
            {
                Author = current.Author,
                ImageUrl = imagePath,
                Date = current.Date,
                IsMe = current.IsMe
            };
            messages.Add(imgMsg);

            var textMsg = new Message
            {
                Author = current.Author,
                Text = remainingText,
                Date = current.Date,
                IsMe = current.IsMe
            };
            messages.Add(textMsg);

            // Evitar añadir el objeto `current` original más abajo
            current = null;
        }
        else
        {
            current.ImageUrl = imagePath;
            current.Text = null;
        }

        return current;
    }

    public string GenerateQrBase64(string url)
    {
        using var qrGenerator = new QRCodeGenerator();
        var payload = new PayloadGenerator.Url(url);
        using var qrCodeData = QRCodeGenerator.GenerateQrCode(payload);
        var qrCode = new PngByteQRCode(qrCodeData);

        var qrBytes = qrCode.GetGraphic(40);
        var base64 = Convert.ToBase64String(qrBytes);

        return $"data:image/png;base64,{base64}";
    }
}
