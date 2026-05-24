using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Playwright;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
       [HttpGet]
    public  IActionResult Get()
    {
        var parser = new ChatParser();

        var messages = parser.Parse("data/_chat.txt", "Eva :')");
        return Ok(messages);
    }

    [HttpGet("pdf")]
    public async Task<IActionResult> GetPdf()
    {
        var parser = new ChatParser();

        var messages = parser.Parse("data/chat.txt", "Sergito 🤍","1/1/22");
        var html = GenerateHtml(messages);

        await GeneratePdf(html);

        var bytes = System.IO.File.ReadAllBytes("chat.pdf");

        return File(bytes, "application/pdf", "chat.pdf");
    }

  public string GenerateHtml(List<MessagesByDate> messages)
    {
        var html =  System.IO.File.ReadAllText("templates/chat.html");

        var sb = new StringBuilder();

        foreach (var msg in messages.SelectMany(x=>x.messages))
        {
            var cssClass = msg.IsMe ? "me" : "other";

            sb.Append($@"
            <div class='message {cssClass}'>
                <div>{System.Net.WebUtility.HtmlEncode(msg.Text)}</div>
                <div class='meta'>{msg.Author}</div>
            </div>
            ");
        }

        return html.Replace("{{MESSAGES}}", sb.ToString());
    }
  
    public async Task GeneratePdf(string html)
    {
        using var playwright = await Playwright.CreateAsync();

        var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true
        });

        var page = await browser.NewPageAsync();

        await page.SetContentAsync(html);

        await page.PdfAsync(new()
        {
            Path = "chat.pdf",
            Format = "A4"
        });

        await browser.CloseAsync();
    }  
}