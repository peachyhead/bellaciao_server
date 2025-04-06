using Microsoft.AspNetCore.Mvc;
using Context;

[ApiController]
[Route("api/user")]
public class UserController : ControllerBase
{
    private readonly MyClassroomContext _context;

    public UserController(MyClassroomContext context)
    {
        _context = context;
    }

    [HttpPost("add")]
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
            .Select(cu => cu.ClassroomID)
            .ToHashSet();

        var availableClassrooms = allClassrooms
            .Where(c => !userClassrooms.Contains(c.ID))
            .ToList();

        return Ok(availableClassrooms);
    }
}