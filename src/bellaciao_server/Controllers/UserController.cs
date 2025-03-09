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

    [HttpGet("get")]
    public ActionResult<User> GetUserById([FromQuery] string id)
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
}