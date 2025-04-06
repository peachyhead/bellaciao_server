using Context;
using Microsoft.AspNetCore.Mvc;

using static ClassroomController;

using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

[ApiController]
[Route("api/classroom/{room_id}/user")]
public class ClassroomUserController : ControllerBase
{
    private readonly MyClassroomContext _context;

    public ClassroomUserController(MyClassroomContext context)
    {
        _context = context;
    }

    [HttpDelete("{id}/remove")]
    public ActionResult Remove(string room_id, string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest("User ID is required.");
        }

        var user = _context.Users.FirstOrDefault(u => u.ID == id);
        if (user == null)
        {
            return NotFound("User not found in any classroom.");
        }

        var classUser = _context.ClassUsers.FirstOrDefault(u => u.UserID == id && u.ClassroomID == room_id);
        if (classUser == null)
        {
            return NotFound($"User {id} not found in classroom {room_id}");
        }

        var participant = _context.LessonParticipants.FirstOrDefault(lp => lp.UserID == id);
        if (participant != null)
        {
            _context.LessonParticipants.Remove(participant);
        }

        var cases = _context.LessonCases.Where(lc => lc.StudentID == id).ToList();
        foreach (var lessonCase in cases)
        {
            lessonCase.StudentID = user.FullName;
        }

        _context.ClassUsers.Remove(classUser);
        _context.SaveChanges();

        return Ok(new { message = "User removed from the classroom.", deleted_user = id, classroom = id} );
    }

    [HttpPost("{id}/edit")]
    public ActionResult Edit(string room_id, string id, [FromBody] ClassUserEditRequest editRequest)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(room_id))
        {
            return BadRequest("User ID and Classroom ID is required.");
        }

        var user = _context.Users.FirstOrDefault(u => u.ID == id);
        if (user == null)
        {
            return NotFound($"User {id} not found any classroom");
        }

        var classuser = _context.ClassUsers.FirstOrDefault(u => u.UserID == id && u.ClassroomID == room_id);
        if (classuser == null)
        {
            return NotFound($"User {id} not found in classroom {room_id}.");
        }

        classuser.Charge = editRequest.Charge;
        _context.ClassUsers.Update(classuser);
        _context.SaveChanges();
        return Ok(new {message = $"User {id} edited successfully."});
    }

    [HttpGet("{id}/get")]
    public ActionResult<ClassUser> GetUserById(string room_id, string id)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(room_id))
        {
            return BadRequest("User ID and Classroom ID is required.");
        }

        var user = _context.Users.FirstOrDefault(u => u.ID == id);
        if (user == null)
        {
            return NotFound("User not found in any classroom.");
        }

        var classuser = _context.ClassUsers.FirstOrDefault(u => u.UserID == id && 
            u.ClassroomID == room_id);

        if (classuser == null)
        {
            return NotFound($"User {id} not found in classroom {room_id}.");
        }

        return Ok(classuser);
    }


    [HttpGet("get")]
    public ActionResult<object> GetUsersByRole(string room_id, [FromQuery] string role)
    {
        if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(room_id))
        {
            return BadRequest("role and room_id parameters are required.");
        }

        var userIds = _context.ClassUsers
            .Where(cu => cu.ClassroomID == room_id && cu.Role == role)
            .Select(cu => cu.UserID)
            .ToList();

        return Ok(new { users = userIds });
    }
}