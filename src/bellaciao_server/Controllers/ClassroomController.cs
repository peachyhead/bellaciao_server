using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Context;
using System.Collections.Generic;
using System.Linq;

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

    /*[HttpGet]
    public ActionResult<List<Classroom>> GetAllClassrooms()
    {
        return Ok(_context.Classrooms.Include(c => c.Participants).ToList());
    }*/

    [HttpPost("create-classroom")]
    public ActionResult CreateClassroom([FromBody] Classroom classroom)
    {
        _context.Classrooms.Add(classroom);
        _context.SaveChanges();
        return CreatedAtAction(nameof(GetAllClassrooms), new { id = classroom.id }, classroom);
    }

    [HttpPost("add-user")]
    public ActionResult AddUser([FromBody] User user)
    {
        _context.Users.Add(user);
        _context.SaveChanges();
        return CreatedAtAction(nameof(GetUserById), new { id = user.id }, user);
    }

    [HttpGet("get-user/{id}")]
    public ActionResult<User> GetUserById(int id)
    {
        var user = _context.Users.Find(id);
        if (user == null)
        {
            return NotFound();
        }
        return Ok(user);
    }
}