// ============================================================
// 1) Servicio singleton que mantiene Playwright + Browser vivos
//    Registrado en Program.cs:
//       builder.Services.AddSingleton<PdfBrowserService>();
//    llama a app.Services.GetRequiredService<PdfBrowserService>().InitializeAsync()
//    una vez al arrancar (o usa un IHostedService, ver más abajo).
// ============================================================
using Microsoft.Playwright;
using System.IO.Compression;
using System.Text;
using Microsoft.AspNetCore.Mvc;

public class PdfBrowserService : IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public async Task<IBrowser> GetBrowserAsync()
    {
        if (_browser is not null)
            return _browser;

        await _initLock.WaitAsync();
        try
        {
            // Doble check por si dos requests llegaron a la vez
            if (_browser is null)
            {
                _playwright = await Playwright.CreateAsync();
                _browser = await _playwright.Chromium.LaunchAsync(new()
                {
                    Headless = true
                });
            }
        }
        finally
        {
            _initLock.Release();
        }

        return _browser;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }
}

// ============================================================
// 2) Hosted service opcional para "precalentar" el browser al
//    arrancar la app, así la primera petición real no paga el
//    coste de arranque. Registrado con:
//       builder.Services.AddHostedService<PdfBrowserWarmup>();
// ============================================================
public class PdfBrowserWarmup : IHostedService
{
    private readonly PdfBrowserService _pdfBrowserService;
    public PdfBrowserWarmup(PdfBrowserService pdfBrowserService) => _pdfBrowserService = pdfBrowserService;

    public Task StartAsync(CancellationToken cancellationToken) => _pdfBrowserService.GetBrowserAsync();
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

// ============================================================
// 3) Controller
// ============================================================

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private static List<MessagesByDate>? cachedMessages;
    private static readonly object cacheLock = new object();
    private readonly PdfBrowserService _pdfBrowserService;
    private readonly IWebHostEnvironment _env;

    public ChatController(PdfBrowserService pdfBrowserService, IWebHostEnvironment env)
    {
        _pdfBrowserService = pdfBrowserService;
        _env = env;
    }

    [HttpGet]
    public IActionResult Get()
    {
        if (cachedMessages != null)
            return Ok(cachedMessages);

        lock (cacheLock)
        {
            if (cachedMessages == null)
            {
                var parser = new ChatParser();
                cachedMessages = parser.Parse("data/chat.txt", "nombre", "20/8/22", "1/9/22");
            }
        }

        return Ok(cachedMessages);
    }

    [HttpPost("pdf")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> GetPdf()
    {
        string htmlbody;
        var contentEncoding = Request.Headers["X-Content-Encoding"].ToString();

        if (contentEncoding.Equals("gzip", StringComparison.OrdinalIgnoreCase))
        {
            using var gzip = new GZipStream(Request.Body, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip, Encoding.UTF8);
            htmlbody = await reader.ReadToEndAsync();
        }
        else
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            htmlbody = await reader.ReadToEndAsync();
        }

        if (string.IsNullOrWhiteSpace(htmlbody))
            return BadRequest("Empty body");

        var pdfBytes = await GeneratePdfAsync(htmlbody);

        return File(pdfBytes, "application/pdf", "chat.pdf");
    }

    private async Task<byte[]> GeneratePdfAsync(string htmlbody)
    {
        var browser = await _pdfBrowserService.GetBrowserAsync();

        // Página aislada por request (barata, no confundir con el browser)
        var page = await browser.NewPageAsync();
        try
        {
            var cssPath = Path.Combine(_env.WebRootPath, "styles.css");
            var css = await System.IO.File.ReadAllTextAsync(cssPath);
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

            // Sin "Path": Playwright devuelve los bytes directamente,
            // sin tocar disco -> elimina la colisión entre requests.
            var pdfBytes = await page.PdfAsync(new()
            {
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

            return pdfBytes;
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}