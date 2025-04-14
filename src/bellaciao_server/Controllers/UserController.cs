using Microsoft.AspNetCore.Mvc;
using Context;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/user")]
public class UserController : ControllerBase
{
    private readonly MyClassroomContext _context;

    public UserController(MyClassroomContext context)
    {
        _context = context;
    }

    [HttpPut("add")]
    public ActionResult AddUser([FromBody] User user)
    {
        _context.Users.Add(user);
        _context.SaveChanges();
        return CreatedAtAction(nameof(GetUserById), new { user.ID }, user);
    }

    [HttpGet("{id}/get")]
    public ActionResult<User> GetUserById(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest("ID is required.");
        }

        var user = _context.Users.Find(id);
        if (user == null)
        {
            return NotFound();
        }
        return Ok(user);
    }

    [HttpGet("{id}/available-classrooms")]
    public ActionResult<List<Classroom>> GetAvailableClassrooms(string id)
    {
        if (string.IsNullOrEmpty(id))
            return BadRequest("User ID is required.");

        var allClassrooms = _context.Classrooms.ToList();
        var userClassrooms = _context.ClassUsers
            .Where(cu => cu.UserID == id)
            .Select(cu => cu.ClassroomID);

        var availableClassrooms = allClassrooms
            .Where(c => userClassrooms.Contains(c.ID))
            .ToList();

        return Ok(availableClassrooms);
    }

    [HttpGet("{id}/chats")]
    public async Task<IActionResult> GetChats(string id)
    {
        var chatsParticipant = await _context.ChatParticipants
            .Where(c => c.UserID == id)
            .ToListAsync();
        if (chatsParticipant == null || !chatsParticipant.Any())
        {
            return NotFound("No chats found for this user.");
        }

        var response = chatsParticipant
            .Select(c => {
                var chat = _context.Chats
                    .Where(ch => ch.ID == c.ChatID)
                    .FirstOrDefault();
                if (chat == null)
                {
                    return null;
                }
                var response = new ChatResponse
                {
                    Chat = chat,
                    Users = _context.ChatParticipants
                        .Where(cp => cp.ChatID == c.ChatID)
                        .Select(cp => cp.UserID)
                        .ToList(),
                    LastMessageViewed = c.LastMessageViewed
                };
                return response;
            });
        
        return Ok(response);
    }
}

public class ChatResponse
{
    public Chat Chat { get; set; }
    public List<string> Users { get; set; }
    public string LastMessageViewed { get; set; }
}