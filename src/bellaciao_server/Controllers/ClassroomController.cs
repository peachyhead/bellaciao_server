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

    [HttpGet]
    public ActionResult<List<Classroom>> GetAllClassrooms()
    {
        return Ok(_context.Classrooms.ToList());
    }

    [HttpPost("create")]
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

    [HttpPost("delete")]
    public ActionResult DeleteClassroom([FromQuery] string classroom_id)
    {
        if (string.IsNullOrEmpty(classroom_id))
        {
            return BadRequest(new { message = "Classroom ID is required." });
        }

        var classroom = _context.Classrooms.FirstOrDefault(c => c.ID == classroom_id);
        if (classroom == null)
        {
            return NotFound(new { message = "Classroom not found." });
        }

        var classUsers = _context.ClassUsers.Where(cu => cu.ClassroomID == classroom_id);
        if (classUsers.Any()) _context.ClassUsers.RemoveRange(classUsers);

        var invitations = _context.Invitations.Where(i => i.ClassroomID == classroom_id);
        if (invitations.Any()) _context.Invitations.RemoveRange(invitations);

        var lessons = _context.Lessons.Where(l => l.RoomID == classroom_id).ToList();
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

        return Ok(new { message = "Classroom and all related records deleted successfully.", classroom_id });
    }

    [HttpPost("edit-user")]
    public ActionResult EditUser([FromQuery] string user_id, [FromQuery] string room_id, 
        [FromBody] ClassUserEditRequest editRequest)
    {
        if (string.IsNullOrEmpty(user_id))
        {
            return BadRequest("User ID is required.");
        }

        var user = _context.Users.FirstOrDefault(u => u.ID == user_id);
        if (user == null)
        {
            return NotFound($"User {user_id} not found any classroom");
        }

        var classuser = _context.ClassUsers.FirstOrDefault(u => u.UserID == user_id && u.ClassroomID == room_id);
        if (classuser == null)
        {
            return NotFound($"User not found in classroom {room_id}.");
        }

        classuser.Charge = editRequest.Charge;
        _context.SaveChanges();

        return Ok(new { message = "User charge updated successfully.", user_id = classuser.UserID, charge = classuser.Charge });
    }

    [HttpPost("remove-user")]
    public ActionResult RemoveUser([FromQuery] string user_id, [FromQuery] string classroom_id)
    {
        if (string.IsNullOrEmpty(user_id))
        {
            return BadRequest("User ID is required.");
        }

        var user = _context.Users.FirstOrDefault(u => u.ID == user_id);
        if (user == null)
        {
            return NotFound("User not found in any classroom.");
        }

        var classUser = _context.ClassUsers.FirstOrDefault(u => u.UserID == user_id && u.ClassroomID == classroom_id);
        if (classUser == null)
        {
            return NotFound($"User not found in classroom {classroom_id}");
        }

        var participant = _context.LessonParticipants.FirstOrDefault(lp => lp.UserID == user_id);
        if (participant != null)
        {
            _context.LessonParticipants.Remove(participant);
        }

        var cases = _context.LessonCases.Where(lc => lc.StudentID == user_id).ToList();
        foreach (var lessonCase in cases)
        {
            lessonCase.StudentID = user.FullName;
        }

        _context.ClassUsers.Remove(classUser);
        _context.SaveChanges();

        return Ok(new { message = "User removed from the classroom.", user_id = classUser.UserID });
    }

    [HttpGet("get-user")]
    public ActionResult<ClassUser> GetUserById([FromQuery] string room_id, [FromQuery] string user_id)
    {
        if (string.IsNullOrEmpty(user_id))
        {
            return BadRequest("User ID is required.");
        }

        var user = _context.Users.FirstOrDefault(u => u.ID == user_id);
        if (user == null)
        {
            return NotFound("User not found in any classroom.");
        }

        var classuser = _context.ClassUsers.FirstOrDefault(u => u.UserID == user_id && 
            u.ClassroomID == room_id);

        if (classuser == null)
        {
            return NotFound("User not found in this classroom.");
        }

        return Ok(classuser);
    }

    [HttpPost("invite")]
    public ActionResult<InviteResponse> InviteUser([FromBody] InviteRequest request)
    {
        if (string.IsNullOrEmpty(request.role) || string.IsNullOrEmpty(request.classroom_id))
        {
            return BadRequest("Role and Classroom ID are required.");
        }

        var invitationId = Guid.NewGuid().ToString();

        var invite = new Invitation
        {
            ID = invitationId,
            Role = request.role,
            ClassroomID = request.classroom_id
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
            Charge = default
        };

        _context.ClassUsers.Add(classUser);
        _context.Invitations.Remove(invitation);
        _context.SaveChanges();

        return Ok(new { message = "User successfully added to the classroom.", classroom_id = classUser.ClassroomID });
    }

    [HttpGet("get-available")]
    public ActionResult<GetAvailableResponse> GetAvailableClassrooms([FromQuery] string user_id)
    {
        if (string.IsNullOrEmpty(user_id))
        {
            return BadRequest("User ID is required.");
        }

        var roomIds = _context.ClassUsers
            .Where(cu => cu.UserID == user_id)
            .Select(cu => cu.ClassroomID)
            .ToList();

        return Ok(new GetAvailableResponse { RoomIds = roomIds });
    }

    [HttpGet("get-users")]
    public ActionResult<object> GetUsersByRole([FromQuery] string role, [FromQuery] string room_id)
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

    public class ClassroomRequest
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("head_id")]
        public string HeadID { get; set; }
    }

    public class GetAvailableResponse
    {
        public List<string> RoomIds { get; set; }
    }

    public class FollowRequest
    {
        public string user_id { get; set; }
    }

    public class InviteRequest
    {
        public string role { get; set; }
        public string classroom_id { get; set; }
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