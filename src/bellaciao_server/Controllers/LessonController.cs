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
            Id = Guid.NewGuid().ToString(),
            RoomId = room_id,
            TemplateId = request.TemplateId,
            TemplateArg = request.TemplateArg,
            Duration = request.Duration,
            Platform = request.Platform,
            CreatedAt = DateTime.UtcNow
        };

        _context.Lessons.Add(lesson);
        _context.SaveChanges();

        foreach (var studentId in request.StudentIds)
        {
            var lessonParticipant = new LessonParticipant
            {
                LessonId = lesson.Id,
                UserId = studentId,
                Role = "student"
            };

            _context.LessonParticipants.Add(lessonParticipant);
        }

        var teacherParticipant = new LessonParticipant
        {
            LessonId = lesson.Id,
            UserId = request.TeacherId,
            Role = "teacher"
        };

        _context.LessonParticipants.Add(teacherParticipant);
        _context.SaveChanges();

        return Ok(new { lesson.Id });
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
    public string Id { get; set; }
    public string RoomId { get; set; }
    public int TemplateId { get; set; }
    public long TemplateArg { get; set; }
    public int Duration { get; set; }
    public string Platform { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LessonParticipant
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string LessonId { get; set; }
    public string UserId { get; set; }
    public string Role { get; set; }
}