using Microsoft.AspNetCore.Mvc;
using Context;

[ApiController]
[Route("api/[controller]")]
public class ClassroomController : ControllerBase
{
    private readonly MyClassroomContext _context;

    public ClassroomController(MyClassroomContext context)
    {
        _context = context;
    }

    [HttpGet("test")]
    public ActionResult<string> Test()
    {
        return Ok("Test request successful!");
    }

    [HttpGet]
    public ActionResult<List<Classroom>> GetAllClassrooms()
    {
        return Ok(_context.Classrooms.ToList());
    }

    [HttpPost("create-classroom")]
    public ActionResult CreateClassroom([FromBody] Classroom classroom)
    {
        _context.Classrooms.Add(classroom);
        _context.SaveChanges();
        return CreatedAtAction(nameof(GetAllClassrooms), new { classroom.id }, classroom);
    }

    [HttpPost("add-user")]
    public ActionResult AddUser([FromBody] User user)
    {
        _context.Users.Add(user);
        _context.SaveChanges();
        return CreatedAtAction(nameof(GetUserById), new { user.id }, user);
    }

    [HttpGet("get-user")]
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