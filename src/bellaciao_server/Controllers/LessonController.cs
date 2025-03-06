using Context;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/lesson")]
public class LessonController : ControllerBase
{
    private readonly MyClassroomContext _context;

    public LessonController(MyClassroomContext context)
    {
        _context = context;
    }

    [HttpPost("add")]
    public ActionResult AddLesson([FromQuery] string room_id, [FromBody] LessonRequest request)
    {
        var lesson = new Lesson
        {
            ID = Guid.NewGuid().ToString(),
            RoomID = room_id,
            TemplateID = request.TemplateId,
            TemplateArg = request.TemplateArg,
            Duration = request.Duration,
            Platform = request.Platform,
        };

        _context.Lessons.Add(lesson);
        _context.SaveChanges();

        foreach (var studentId in request.StudentIds)
        {
            var lessonParticipant = new LessonParticipant
            {
                LessonID = lesson.ID,
                UserID = studentId,
                Role = "student"
            };

            _context.LessonParticipants.Add(lessonParticipant);
        }

        var teacherParticipant = new LessonParticipant
        {
            LessonID = lesson.ID,
            UserID = request.TeacherId,
            Role = "teacher"
        };

        _context.LessonParticipants.Add(teacherParticipant);
        _context.SaveChanges();

        return Ok(new { lesson.ID });
    }
}

// DTO для запроса
public class LessonRequest
{
    public int TemplateId { get; set; }
    public long TemplateArg { get; set; }
    public int Duration { get; set; }
    public string Platform { get; set; }
    public string TeacherId { get; set; }
    public List<string> StudentIds { get; set; }
}

public class Lesson
{
    public string ID { get; set; }
    public string RoomID { get; set; }
    public int TemplateID { get; set; }
    public long TemplateArg { get; set; }
    public int Duration { get; set; }
    public string Platform { get; set; }
}

public class LessonParticipant
{
    public string UserID { get; set; }
    public string LessonID { get; set; }
    public string Role { get; set; }
}

public class LessonCase
{
    public string LessonID { get; set; }
    public string UserID { get; set; }
    public string Description { get; set; }
    public long CreatedAt { get; set; }
    public long OldDate { get; set; }
    public long NewDate { get; set; }
}