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

    [HttpPost("add")]
    public async Task<IActionResult> AddChat([FromBody] ChatRequest chatRequest)
    {
        var chat = new Chat()
        {
            ID = Guid.NewGuid().ToString(),
            Name = chatRequest.Name,
            LastMessage = chatRequest.LastMessage,
            LastMessageTime = chatRequest.LastMessageTime
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
            .ToListAsync();

        return Ok(messages);
    }

    [HttpPut]
    [Route("{chat_id}/view")]
    public async Task<IActionResult> ViewMessage(string chat_id)
    {
        if (string.IsNullOrEmpty(chat_id))
        {
            return BadRequest("Chat ID is required.");
        }
    
        var message = await _context.Messages
            .Where(m => m.ChatID == chat_id)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync();
        
        if (message == null)
        {
            return NotFound("No messages found in this chat.");
        }

        var chatParticipant = await _context.ChatParticipants
            .Where(cp => cp.ChatID == chat_id)
            .FirstOrDefaultAsync();
        if (chatParticipant == null)
        {
            return NotFound("Chat participant not found.");
        }

        chatParticipant.LastMessageViewed = message.ID;
        await _context.SaveChangesAsync();

        return Ok(message);
    }

    [HttpPut]
    [Route("{chat_id}/messages/edit")]
    public async Task<IActionResult> EditMessage(string chat_id, [FromQuery] string message_id, 
        [FromBody] EditMessageRequest request)
    {
        if (string.IsNullOrEmpty(chat_id))
            return BadRequest("Chat ID is required.");

        var message = await _context.Messages
            .Where(m => m.ID == message_id && m.ChatID == chat_id)
            .FirstAsync();;

        if (message == null)
            return NotFound("Message not found.");

        message.Text = request.Text;
        await _context.SaveChangesAsync();

        return Ok(message);
    }

    [HttpDelete]
    [Route("{chat_id}/messages/delete")]
    public async Task<IActionResult> DeleteMessage(string chat_id, [FromQuery] string message_id)
    {
        if (message_id == null)
            return BadRequest("Chat ID is required.");

        var message = await _context.Messages
            .Where(m => m.ID == message_id && m.ChatID == chat_id)
            .FirstAsync();;

        if (message == null)
            return NotFound("Message not found.");

        _context.Remove(message);
        await _context.SaveChangesAsync();

        return Ok(message);
    }

    [HttpPost("{chat_id}/messages")]
    public async Task<IActionResult> PostMessage(string chat_id, [FromBody] MessageRequest messageRequest)
    {
        if (string.IsNullOrEmpty(chat_id))
        {
            return BadRequest("Chat ID is required.");
        }

        var message = new Message()
        {
            ID = Guid.NewGuid().ToString(),
            Text = messageRequest.Text,
            AuthorID = messageRequest.AuthorID,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ChatID = chat_id
        };

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

public class EditMessageRequest
{
    public string Text { get; set; }
}

public class ChatRequest
{
    public string Name { get; set; }
    public string? LastMessage { get; set; }
    public long? LastMessageTime { get; set; }
}

public class MessageRequest
{
    public string ChatID { get; set; }
    public string AuthorID { get; set; }
    public string Text { get; set; }
    public long CreatedAt { get; set; }
}