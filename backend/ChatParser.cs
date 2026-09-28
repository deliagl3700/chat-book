using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using QRCoder;
public class ChatParser
{
    private static readonly Regex StartRegex = new(@"^\s*\[((?:\d{1,2}\/){2}\d{2,4},\s*\d{1,2}:\d{2}(?::\d{2})?)\]\s(.*?):\s(.*)", RegexOptions.Compiled);
    private static readonly Regex StickerRegex = new(@"<adjunto:\s(.*?\.webp)>", RegexOptions.Compiled);
    private static readonly Regex PhotoRegex = new(@"<adjunto:\s(.*?\.(jpg|jpeg|png))>", RegexOptions.Compiled);
    private static readonly Regex AudioRegex = new(@"<adjunto:\s(.*?\.opus)>", RegexOptions.Compiled);
    private static readonly Regex VideoRegex = new(@"<adjunto:\s(.*?\.mp4)>", RegexOptions.Compiled);

    public List<MessagesByDate> Parse(string path, string myName, string? fromDate = null, string? untilDate = null)
    {
        var lines = File.ReadAllLines(path, Encoding.UTF8);

        var messages = new List<Message>();
        Message? current = null;
        var json = File.ReadAllText("data/mapeo.json");
        var hasFromDate = !string.IsNullOrWhiteSpace(fromDate);
        var reachedFromDate = !hasFromDate;

        var mediaList = JsonSerializer.Deserialize<List<MediaItem>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<MediaItem>();
        var mediaByFileName = mediaList
            .Where(item => !string.IsNullOrWhiteSpace(item.FileName))
            .ToDictionary(item => item.FileName, item => item.Url);
        var imageCache = new Dictionary<string, string>();
        var qrCache = new Dictionary<string, string>();

        foreach (var line in lines)
        {
            var lineClean = line.TrimStart('\u200E', '\u200F', '\u202A', '\u202C');

            if (hasFromDate && !reachedFromDate)
            {
                if (!lineClean.StartsWith($"[{fromDate}", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                reachedFromDate = true;
            }

            // Detener si se encuentra la fecha especificada
            if (untilDate != null && lineClean.StartsWith($"[{untilDate}", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            var match = StartRegex.Match(lineClean);

            if (match.Success)
            {
                var timestamp = match.Groups[1].Value;
                if (!DateTime.TryParseExact(timestamp, new[]
                {
                    "d/M/yy, H:mm:ss",
                    "d/M/yyyy, H:mm:ss",
                    "d/M/yy, H:mm",
                    "d/M/yyyy, H:mm",
                    "d/M/yy, HH:mm:ss",
                    "d/M/yyyy, HH:mm:ss",
                    "d/M/yy, HH:mm",
                    "d/M/yyyy, HH:mm",
                    "M/d/yy, H:mm:ss",
                    "M/d/yyyy, H:mm:ss",
                    "M/d/yy, H:mm",
                    "M/d/yyyy, H:mm",
                    "M/d/yy, HH:mm:ss",
                    "M/d/yyyy, HH:mm:ss",
                    "M/d/yy, HH:mm",
                    "M/d/yyyy, HH:mm"
                }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    current?.Text += "\n" + line;
                    continue;
                }

                current = new Message
                {
                    Author = match.Groups[2].Value,
                    Text = match.Groups[3].Value.Replace("<Se editó este mensaje.>", string.Empty),
                    Date = date,
                    IsMe = match.Groups[2].Value == myName
                };

                var matchSticker = StickerRegex.Match(current.Text);
                var matchPhoto = PhotoRegex.Match(current.Text);
                var matchAudio = AudioRegex.Match(current.Text);
                var matchVideo = VideoRegex.Match(current.Text);

                if (matchPhoto.Success)
                {
                    current = TreatImageData(messages, current, PhotoRegex, matchPhoto, imageCache);
                }
                else if (matchSticker.Success)
                {
                    current.StickerUrl = GetCachedBase64Image($"assets/{matchSticker.Groups[1].Value}", "image/webp", imageCache);
                    current.Text = null;
                } else if (matchAudio.Success)
                {
                    current.AudioUrl = $"{matchAudio.Groups[1].Value}";
                    var url = mediaByFileName.GetValueOrDefault(current.AudioUrl, string.Empty);
                    current.AudioUrl = url;
                    current.QrCode = GetCachedQr(url, qrCache);
                    current.Text = null;
                }else if (matchVideo.Success)
                {
                    current.VideoUrl = $"{matchVideo.Groups[1].Value}";
                    var url = mediaByFileName.GetValueOrDefault(current.VideoUrl, string.Empty);
                    current.VideoUrl = url;
                    current.QrCode = GetCachedQr(url, qrCache);
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

    private static Message? TreatImageData(List<Message> messages, Message? current, Regex photoRegex, Match matchPhoto, Dictionary<string, string> imageCache)
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
                ImageUrl = GetCachedBase64Image(imagePath, "image/jpg", imageCache),
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
            current.ImageUrl = GetCachedBase64Image(imagePath, "image/jpg", imageCache);
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

    private static string GetCachedBase64Image(string filePath, string mimeType, Dictionary<string, string> imageCache)
    {
        if (!imageCache.TryGetValue(filePath, out var image))
        {
            image = ToBase64Image(filePath, mimeType);
            imageCache[filePath] = image;
        }

        return image;
    }

    private string? GetCachedQr(string url, Dictionary<string, string> qrCache)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        if (!qrCache.TryGetValue(url, out var qr))
        {
            qr = GenerateQrBase64(url);
            qrCache[url] = qr;
        }

        return qr;
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
