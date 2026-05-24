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

   [HttpPost("pdf")]
    public async Task<IActionResult> GetPdf()
    {
        string htmlbody;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
            htmlbody = await reader.ReadToEndAsync();
        }

        if (string.IsNullOrWhiteSpace(htmlbody))
        {
            return BadRequest("Empty body");
        }

        await GeneratePdf(htmlbody);

        var bytes = System.IO.File.ReadAllBytes("chat.pdf");

        return File(bytes, "application/pdf", "chat.pdf");
    }

    public async Task GeneratePdf(string htmlbody)
    {
        using var playwright = await Playwright.CreateAsync();

        var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true
        });

        var page = await browser.NewPageAsync();
        var css = System.IO.File.ReadAllText("wwwroot/styles.css");
        var html = $@"
                <html>
                <head>
                <style>
                {css}
                </style>
                </head>
                <body>
                {htmlbody}
                </body>
            </html>";
        await page.SetContentAsync(html);

        await page.PdfAsync(new()
        {
            Path = "chat.pdf",
            Format = "A4",
            PrintBackground = true,
            Margin = new()
            {
                Top = "20mm",
                Bottom = "20mm",
                Left = "10mm",
                Right = "10mm"
            }
        });

        await browser.CloseAsync();
    }  
}