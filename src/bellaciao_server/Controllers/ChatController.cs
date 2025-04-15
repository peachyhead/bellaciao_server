using System.Text.Json.Serialization;
using Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly MyClassroomContext _context;

    public ChatController(MyClassroomContext db)
    {
        _context = db;
    }

    [HttpPost("add")]
    public async Task<IActionResult> AddChat([FromBody] ChatRequest chatRequest)
    {
        var chat = new Chat()
        {
            ID = Guid.NewGuid().ToString(),
            Name = chatRequest.Name,
            LastMessage = null,
            LastMessageTime = null
        };

        _context.Chats.Add(chat);
        await _context.SaveChangesAsync();

        return Ok(chat);
    }

    [HttpGet("{chat_id}/messages")]
    public async Task<IActionResult> GetMessages(string chat_id)
    {
        if (string.IsNullOrEmpty(chat_id))
        {
            return BadRequest("Chat ID is required.");
        }

        var messages = await _context.Messages
            .Where(m => m.ChatID == chat_id)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new MessageResponse
            {
                ID = m.ID,
                Author = _context.Users.FirstOrDefault(u => u.ID == m.AuthorID),
                Text = m.Text,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();

        return Ok(messages);
    }
}

public class EditMessageRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; }
}

public class ChatRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class MessageRequest
{
    [JsonPropertyName("author_id")]
    public string AuthorID { get; set; }
    [JsonPropertyName("text")]
    public string Text { get; set; }
    [JsonPropertyName("created_at")]
    public long CreatedAt { get; set; }
}

public class MessageResponse
{
    [JsonPropertyName("id")]
    public string ID { get; set; }
    [JsonPropertyName("author")]
    public User Author { get; set; }
    [JsonPropertyName("text")]
    public string Text { get; set; }
    [JsonPropertyName("created_at")]
    public long CreatedAt { get; set; }
}