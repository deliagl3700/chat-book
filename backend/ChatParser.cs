using System.Globalization;
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
                    current.StickerUrl = ToBase64Image($"assets/{matchSticker.Groups[1].Value}", "image/webp");
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

        var messagesByDate = messages
        .GroupBy(m => new { m.Date.Year, m.Date.Month })
        .Select(month => new MessagesByDate
        {
        Year = month.Key.Year,
        Month = month.Key.Month,
        dayGroup = month
            .GroupBy(m => m.Date.Date)
            .Select(day => new DayGroup
            {
                Date = day.Key,
                Messages = day.ToList()
            })
            .ToList()
        })
        .ToList();
         return messagesByDate;
    }

    private static Message? TreatImageData(List<Message> messages, Message? current, Regex photoRegex, Match matchPhoto)
    {
        var imagePath = $"assets/{matchPhoto.Groups[1].Value}";

        // Eliminar solo la etiqueta de adjunto y obtener el texto restante.
        // Quitar caracteres invisibles de formato y normalizar espacios.
        var remainingText = photoRegex.Replace(current.Text ?? string.Empty, string.Empty);
        remainingText = Regex.Replace(remainingText, @"\p{Cf}", string.Empty);
        remainingText = Regex.Replace(remainingText, @"[\s]+", " ").Trim();
        remainingText = remainingText.Length == 0 ? null : remainingText;

        if (remainingText != null)
        {
            var imgMsg = new Message
            {
                Author = current.Author,
                ImageUrl = ToBase64Image(imagePath, "image/jpg"),
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
            current.ImageUrl = ToBase64Image(imagePath, "image/jpg");
            current.Text = null;
        }

        return current;
    }

    public static string ToBase64Image(string filePath, string mimeType)
    {
        var bytes = File.ReadAllBytes(filePath);
        var base64 = Convert.ToBase64String(bytes);
        return $"data:{mimeType};base64,{base64}";
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
