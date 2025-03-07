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
            TemplateID = request.template_id,
            TemplateArg = request.template_arg,
            Duration = request.duration,
            Platform = request.platform,
        };

        _context.Lessons.Add(lesson);
        _context.SaveChanges();

        foreach (var studentId in request.student_ids)
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
            UserID = request.teacher_id,
            Role = "teacher"
        };

        _context.LessonParticipants.Add(teacherParticipant);
        _context.SaveChanges();

        return Ok(new { lesson.ID });
    }

    [HttpGet("get-all")]
    public ActionResult<IEnumerable<LessonResponse>> GetLessonsByRoomId([FromQuery] string room_id)
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
            StudentIDs = _context.LessonParticipants
                .Where(lp => lp.LessonID == l.ID && lp.Role == "student")
                .Select(lp => lp.UserID)
                .ToList(),
            TemplateID = l.TemplateID,
            TemplateArg = l.TemplateArg,
            Cases = _context.LessonCases
                .Where(c => c.LessonID == l.ID)
                .Select(c => new LessonCaseResponse
                {
                    Type = c.Type,
                    UserID = c.UserID,
                    Date = c.CreatedAt,
                    Description = c.Description,
                    OldDate = c.OldDate,
                    NewDate = c.NewDate
                })
                .ToList()
        })
        .ToList();

    return Ok(lessons);
}
}

// DTO для ответа
public class LessonResponse
{
    public string LessonID { get; set; }
    public string TeacherID { get; set; }
    public string Platform { get; set; }
    public List<string> StudentIDs { get; set; }
    public int TemplateID { get; set; }
    public string TemplateArg { get; set; }
    public List<LessonCaseResponse> Cases { get; set; }
}

public class LessonCaseResponse
{
    public string Type { get; set; }
    public string UserID { get; set; }
    public long Date { get; set; }
    public string Description { get; set; }
    public long CreatedAt { get; set; }
    public long OldDate { get; set; }
    public long NewDate { get; set; }
}

// DTO для запроса
public class LessonRequest
{
    public int template_id { get; set; }
    public string template_arg { get; set; }
    public int duration { get; set; }
    public string platform { get; set; }
    public string teacher_id { get; set; }
    public List<string> student_ids { get; set; }
}

public class Lesson
{
    public string ID { get; set; }
    public string RoomID { get; set; }
    public int TemplateID { get; set; }
    public string TemplateArg { get; set; }
    public int Duration { get; set; }
    public string Platform { get; set; }
}

public class LessonParticipant
{
    public int ID { get; set; }
    public string UserID { get; set; }
    public string LessonID { get; set; }
    public string Role { get; set; }
}

public class LessonCase
{
    public int ID { get; set; }
    public string Type { get; set; }
    public string LessonID { get; set; }
    public string UserID { get; set; }
    public string Description { get; set; }
    public long CreatedAt { get; set; }
    public long OldDate { get; set; }
    public long NewDate { get; set; }
}