using System.Text.Json.Serialization;
using Context;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/classroom/{room_id}/lesson")]
public class LessonController : ControllerBase
{
    private readonly MyClassroomContext _context;

    public LessonController(MyClassroomContext context)
    {
        _context = context;
    }

    [HttpPost("add")]
    public ActionResult AddLesson(string room_id, [FromBody] LessonRequest request)
    {
        if (string.IsNullOrEmpty(room_id))
        {
            return BadRequest(new { message = "Room ID is required" });
        }

        if (request == null || string.IsNullOrEmpty(request.TeacherID))
        {
            return BadRequest(new { message = "Invalid request data" });
        }

        var lesson = new Lesson
        {
            ID = Guid.NewGuid().ToString(),
            RoomID = room_id,
            PeriodicType = request.PeriodicType,
            PeriodicTime = request.PeriodicTime,
            Duration = request.Duration,
            Platform = request.Platform,
        };

        var participants = new List<LessonParticipant>();

        var teacher = _context.Users.Find(request.TeacherID);
        if (teacher == null)
        {
            return NotFound(new { message = "Teacher not found" });
        }

        var classuser = _context.ClassUsers.FirstOrDefault(cu => cu.UserID == request.TeacherID);
        if (classuser == null)
        {
            return NotFound(new { message = "Teacher is not attended in this classroom" });
        }

        participants.Add(new LessonParticipant
        {
            LessonID = lesson.ID,
            UserID = request.TeacherID,
            Role = "teacher"
        });

        if (request.StudentIDs.Any())
        {
            foreach (var studentId in request.StudentIDs)
            {
                var student = _context.Users.Find(studentId);
                if (student == null)
                {
                    return NotFound(new { message = $"Student with ID {studentId} not found" });
                }

                var classstud = _context.ClassUsers.FirstOrDefault(cu => cu.UserID == studentId);
                if (classstud == null)
                {
                    return NotFound(new { message = "Student is not attended in this classroom" });
                }

                participants.Add(new LessonParticipant
                {
                    LessonID = lesson.ID,
                    UserID = studentId,
                    Role = "student"
                });
            }
        }
        
        _context.Lessons.Add(lesson);
        _context.SaveChanges();
        _context.LessonParticipants.AddRange(participants);
        _context.SaveChanges();

        return Ok(new { lesson.ID });
    }

    [HttpPost("{id}/edit")]
    public ActionResult EditLesson(string room_id, string id, [FromBody] LessonEditRequest editRequest)
    {
        var lesson = _context.Lessons.FirstOrDefault(l => l.ID == id && 
            l.RoomID == room_id);
        if (lesson == null)
        {
            return NotFound(new { message = "Lesson not found" });
        }

        if (editRequest.PeriodicTime != 0)
        {
            lesson.PeriodicTime = editRequest.PeriodicTime;
        }

        if (editRequest.Duration != 0)
        {
            lesson.Duration = editRequest.Duration;
        }

        if (!string.IsNullOrEmpty(editRequest.Platform))
        {
            lesson.Platform = editRequest.Platform;
        }

        if (!string.IsNullOrEmpty(editRequest.TeacherID))
        {
            var classuser = _context.ClassUsers.FirstOrDefault(cu => cu.UserID == editRequest.TeacherID);
            if (classuser == null)
            {
                return NotFound(new { message = "Student is not attended in this classroom" });
            }

            var teacherParticipant = _context.LessonParticipants
                .FirstOrDefault(lp => lp.LessonID == id && lp.Role == "teacher");
            if (teacherParticipant != null)
            {
                teacherParticipant.UserID = editRequest.TeacherID;
            }
            else
            {
                var newTeacherParticipant = new LessonParticipant
                {
                    LessonID = id,
                    UserID = editRequest.TeacherID,
                    Role = "teacher"
                };
                _context.LessonParticipants.Add(newTeacherParticipant);
            }
        }

        if (editRequest.StudentsIncluded.Any())
        {
            foreach (var studentId in editRequest.StudentsIncluded)
            {
                var classuser = _context.ClassUsers.FirstOrDefault(cu => cu.UserID == studentId);
                if (classuser == null)
                {
                    return NotFound(new { message = "Student is not attended in this classroom" });
                }

                var studentParticipant = _context.LessonParticipants
                    .FirstOrDefault(lp => lp.LessonID == id && lp.UserID == studentId &&
                                         lp.Role == "student");

                if (studentParticipant == null)
                {
                    var newStudentParticipant = new LessonParticipant
                    {
                        LessonID = id,
                        UserID = studentId,
                        Role = "student"
                    };
                    _context.LessonParticipants.Add(newStudentParticipant);
                }
                else
                {
                    return BadRequest(new { message = "Student is already in lesson" });
                }
            }
        }

        if (editRequest.StudentsExcluded.Any())
        {
            foreach (var studentId in editRequest.StudentsExcluded)
            {
                var classuser = _context.ClassUsers.FirstOrDefault(cu => cu.UserID == studentId);
                if (classuser == null)
                {
                    return NotFound(new { message = "Student is not attended in this classroom" });
                }

                var studentParticipant = _context.LessonParticipants
                    .FirstOrDefault(lp => lp.LessonID == id && lp.UserID == studentId);
                if (studentParticipant != null)
                {
                    _context.LessonParticipants.Remove(studentParticipant);
                }
                else
                {
                    return BadRequest(new { message = "Student is not in lesson" });
                }
            }
        }

        _context.SaveChanges();
        return Ok(new { message = "Lesson updated successfully" });
    }

    [HttpGet("get")]
    public ActionResult<IEnumerable<LessonResponse>> GetLessonsByRoomId(string room_id)
    {
        var lessons = _context.Lessons
            .Where(l => l.RoomID == room_id)
            .Select(l => new LessonResponse
            {
                LessonID = l.ID,
                TeacherID = _context.LessonParticipants
                    .Where(lp => lp.LessonID == l.ID && lp.Role == "teacher")
                    .Select(lp => lp.UserID)
                    .First(),
                Platform = l.Platform,
                Duration = l.Duration,
                StudentIDs = _context.LessonParticipants
                    .Where(lp => lp.LessonID == l.ID && lp.Role == "student")
                    .Select(lp => lp.UserID)
                    .ToList(),
                PeriodicType = l.PeriodicType,
                PeriodicTime = l.PeriodicTime,
                Cases = _context.LessonCases
                    .Where(c => c.LessonID == l.ID)
                    .ToList()
        })
        .ToList();

        return Ok(lessons);
    }
}

public class LessonCaseRequest
{
    public required string Type { get; set; }
    
    [JsonPropertyName("student_id")]
    public required string StudentID { get; set; }
    public required string Description { get; set; }
    public required long CreatedAt { get; set; }
    public required long LessonDate { get; set; }
    public string? PeriodicType { get; set; }
    public long? PeriodicTime { get; set; }
}

public class LessonResponse
{
    public string LessonID { get; set; }
    public string TeacherID { get; set; }
    public string Platform { get; set; }
    public List<string> StudentIDs { get; set; }
    public string PeriodicType { get; set; }
    public long PeriodicTime { get; set; }
    public int Duration { get; set; }
    public List<LessonCase> Cases { get; set; }
}

public class LessonCaseResponse
{
    public required string ID { get; set; }
    public required string Type { get; set; }
    public required string UserID { get; set; }
    public string? Description { get; set; }
    public long CreatedAt { get; set; }
    public long? LessonDate { get; set; }
    public string? PeriodicType { get; set; }
    public long? PeriodicTime { get; set; }
}

public class LessonRequest
{
    [JsonPropertyName("periodic_type")]
    public string PeriodicType { get; set; }
    [JsonPropertyName("periodic_time")]
    public long PeriodicTime { get; set; }
    [JsonPropertyName("duration")]
    public int Duration { get; set; }
    [JsonPropertyName("platform")]
    public string Platform { get; set; }
    [JsonPropertyName("teacher_id")]
    public string TeacherID { get; set; }
    [JsonPropertyName("student_ids")]
    public List<string> StudentIDs { get; set; }
}

public class LessonEditRequest
{
    [JsonPropertyName("periodic_time")]
    public long PeriodicTime { get; set; }
    public int Duration { get; set; }
    public string? Platform { get; set; }
    [JsonPropertyName("teacher_id")]
    public string? TeacherID { get; set; }
    [JsonPropertyName("students_included")]
    public List<string> StudentsIncluded { get; set; }
    [JsonPropertyName("students_excluded")]
    public List<string> StudentsExcluded { get; set; }
}