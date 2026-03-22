using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var parser = new ChatParser();

        var messages = parser.Parse("data/chat.txt", "Sergito 🤍","10/1/22");

        return Ok(messages);
    }
}