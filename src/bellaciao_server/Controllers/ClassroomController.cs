using Microsoft.AspNetCore.Mvc;
using Context;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/classroom")]
public class ClassroomController : ControllerBase
{
    private readonly MyClassroomContext _context;

    public ClassroomController(MyClassroomContext context)
    {
        _context = context;
    }

    [HttpGet("get")]
    public ActionResult<List<Classroom>> GetAllClassrooms()
    {
        return Ok(_context.Classrooms.ToList());
    }

    [HttpGet("{id}/get")]
    public ActionResult<Classroom> GetById(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest(new { message = "Classroom ID is required." });
        }

        var classroom = _context.Classrooms.FirstOrDefault(c => c.ID == id);
        if (classroom == null)
        {
            return NotFound(new { message = "Classroom not found." });
        }
        
        return Ok(classroom);
    }

    [HttpPut("add")]
    public ActionResult CreateClassroom([FromBody] ClassroomRequest classroomRequest)
    {
        var classroom = new Classroom
        {
            ID = Guid.NewGuid().ToString(),
            Title = classroomRequest.Title,
            HeadID = classroomRequest.HeadID
        };

        var head = _context.Users.Find(classroom.HeadID);
        if (head == null)
        {
            return NotFound("Head user not found.");
        }

        _context.Classrooms.Add(classroom);
        _context.SaveChanges();

        var classUser = new ClassUser
        {
            ClassroomID = classroom.ID,
            UserID = classroom.HeadID,
            Role = "teacher",
            Charge = default
        };

        _context.ClassUsers.Add(classUser);
        _context.SaveChanges();

        return CreatedAtAction(nameof(GetAllClassrooms), new { classroom.ID }, classroom);
    }

    [HttpDelete("{id}/delete")]
    public ActionResult DeleteClassroom(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest(new { message = "Classroom ID is required." });
        }

        var classroom = _context.Classrooms.FirstOrDefault(c => c.ID == id);
        if (classroom == null)
        {
            return NotFound(new { message = "Classroom not found." });
        }

        var classUsers = _context.ClassUsers.Where(cu => cu.ClassroomID == id);
        if (classUsers.Any()) _context.ClassUsers.RemoveRange(classUsers);

        var invitations = _context.Invitations.Where(i => i.ClassroomID == id);
        if (invitations.Any()) _context.Invitations.RemoveRange(invitations);

        var lessons = _context.Lessons.Where(l => l.RoomID == id).ToList();
        if (lessons.Any())
        {
            var lessonIds = lessons.Select(l => l.ID).ToList();

            var lessonCases = _context.LessonCases.Where(lc => lessonIds.Contains(lc.LessonID)).ToList();
            if (lessonCases.Any())
            {
                var lessonCaseIds = lessonCases.Select(lc => lc.ID).ToList();
                var lessonCaseFiles = _context.LessonCaseFiles.Where(lcf => lessonCaseIds.Contains(lcf.LessonCaseID));
                if (lessonCaseFiles.Any()) _context.LessonCaseFiles.RemoveRange(lessonCaseFiles);

                _context.LessonCases.RemoveRange(lessonCases);
            }

            var lessonParticipants = _context.LessonParticipants.Where(lp => lessonIds.Contains(lp.LessonID));
            if (lessonParticipants.Any()) 
            _context.LessonParticipants.RemoveRange(lessonParticipants);

            _context.Lessons.RemoveRange(lessons);
        }

        _context.Classrooms.Remove(classroom);

        try
        {
            _context.SaveChanges();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return StatusCode(500, new { message = "Error deleting classroom. It may have already been removed.", details = ex.Message });
        }

        return Ok(new { message = "Classroom and all related records deleted successfully.", id });
    }

    [HttpPost("{id}/invite")]
    public ActionResult<InviteResponse> InviteUser(string id, [FromQuery] string role, [FromQuery] decimal charge)
    {
        if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(id))
        {
            return BadRequest("Role and Classroom ID are required.");
        }

        var invitationId = Guid.NewGuid().ToString();

        var invite = new Invitation
        {
            ID = invitationId,
            Role = role,
            ClassroomID = id,
            Charge = charge
        };

        _context.Invitations.Add(invite);
        _context.SaveChanges();

        return Ok(new InviteResponse { invite_id = invitationId });
    }

    [HttpPost("follow")]
    public ActionResult FollowInvitation([FromQuery] string invite_id, [FromBody] FollowRequest request)
    {
        if (string.IsNullOrEmpty(invite_id) || string.IsNullOrEmpty(request.user_id))
        {
            return BadRequest("Invite ID and User ID are required.");
        }

        var invitation = _context.Invitations.FirstOrDefault(i => i.ID == invite_id);
        if (invitation == null)
        {
            return NotFound("Invitation not found.");
        }

        var userExists = _context.ClassUsers.Any(cu => cu.ClassroomID == invitation.ClassroomID && 
            cu.UserID == request.user_id);
        if (userExists)
        {
            return Conflict(new { message = "User is already in the classroom.", classroom_id = invitation.ClassroomID });
        }

        var classUser = new ClassUser
        {
            ClassroomID = invitation.ClassroomID,
            UserID = request.user_id,
            Role = invitation.Role,
            Charge = invitation.Charge
        };

        _context.ClassUsers.Add(classUser);
        _context.Invitations.Remove(invitation);
        _context.SaveChanges();

        return Ok(new { message = "User successfully added to the classroom.", classroom_id = classUser.ClassroomID });
    }

    public class ClassroomRequest
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("head_id")]
        public string HeadID { get; set; }
    }
    
    public class FollowRequest
    {
        public string user_id { get; set; }
    }

    public class InviteResponse
    {
        public string invite_id { get; set; }
    }

    public class ClassUserEditRequest 
    {
        public decimal Charge { get; set; }
    }
}