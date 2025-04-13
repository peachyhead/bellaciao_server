using System.Text.Json;

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
            .ToListAsync();

        return Ok(messages);
    }

    [HttpPost("{chat_id}/messages")]
    public async Task<IActionResult> PostMessage(string chat_id, [FromBody] Message message)
    {
        if (string.IsNullOrEmpty(chat_id))
        {
            return BadRequest("Chat ID is required.");
        }

        message.ID = Guid.NewGuid().ToString();
        message.ChatID = chat_id;
        message.CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        _context.Messages.Add(message);

        var chat = await _context.Chats.FindAsync(chat_id);
        if (chat != null)
        {
            chat.LastMessage = message.Text;
            chat.LastMessageTime = message.CreatedAt;
        }

        await _context.SaveChangesAsync();

        var messageDto = new
        {
            message.ID,
            message.ChatID,
            message.AuthorID,
            message.Text,
            message.CreatedAt
        };

        await WebSocketManager.BroadcastToChatAsync(chat_id, JsonSerializer.Serialize(new
        {
            type = "new_message",
            data = messageDto
        }));

        return Ok(messageDto);
    }
}