using System.Text;
using System.Text.Json.Serialization;

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
            PeriodicType = request.PeriodicType,
            PeriodicTime = request.PeriodicTime,
            Duration = request.Duration,
            Platform = request.Platform,
        };

        _context.Lessons.Add(lesson);
        _context.SaveChanges();

        foreach (var studentId in request.StudentIDs)
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
            UserID = request.TeacherID,
            Role = "teacher"
        };

        _context.LessonParticipants.Add(teacherParticipant);
        _context.SaveChanges();

        return Ok(new { lesson.ID });
    }

    [HttpPost("edit")]
    public ActionResult EditLesson([FromQuery] string lesson_id, [FromBody] LessonEditRequest editRequest)
    {
        var lesson = _context.Lessons.FirstOrDefault(l => l.ID == lesson_id);
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
            var teacherParticipant = _context.LessonParticipants
                .FirstOrDefault(lp => lp.LessonID == lesson_id && lp.Role == "teacher");
            if (teacherParticipant != null)
            {
                teacherParticipant.UserID = editRequest.TeacherID;
            }
            else
            {
                var newTeacherParticipant = new LessonParticipant
                {
                    LessonID = lesson_id,
                    UserID = editRequest.TeacherID,
                    Role = "teacher"
                };
                _context.LessonParticipants.Add(newTeacherParticipant);
            }
        }

        if (editRequest.StudentsIncluded != null)
        {
            foreach (var studentId in editRequest.StudentsIncluded)
            {
                var studentParticipant = _context.LessonParticipants
                    .FirstOrDefault(lp => lp.LessonID == lesson_id && lp.UserID == studentId);
                if (studentParticipant == null)
                {
                    var newStudentParticipant = new LessonParticipant
                    {
                        LessonID = lesson_id,
                        UserID = studentId,
                        Role = "student"
                    };
                    _context.LessonParticipants.Add(newStudentParticipant);
                }
                else
                {
                    return BadRequest(new { message = "Student already in lesson" });
                }
            }
        }

        if (editRequest.StudentsExcluded != null)
        {
            foreach (var studentId in editRequest.StudentsExcluded)
            {
                var studentParticipant = _context.LessonParticipants
                    .FirstOrDefault(lp => lp.LessonID == lesson_id && lp.UserID == studentId);
                if (studentParticipant != null)
                {
                    _context.LessonParticipants.Remove(studentParticipant);
                }
                else
                {
                    return BadRequest(new { message = "Student not in lesson" });
                }
            }
        }

        _context.SaveChanges();
        return Ok(new { message = "Lesson updated successfully" });
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
                Duration = l.Duration,
                StudentIDs = _context.LessonParticipants
                    .Where(lp => lp.LessonID == l.ID && lp.Role == "student")
                    .Select(lp => lp.UserID)
                    .ToList(),
                PeriodicType = l.PeriodicType,
                PeriodicTime = l.PeriodicTime,
                Cases = _context.LessonCases
                    .Where(c => c.LessonID == l.ID)
                    .Select(c => new LessonCaseResponse
                    {
                        Type = c.Type,
                        UserID = c.StudentID,
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

    [HttpPost("add-case")]
    public ActionResult AddLessonCase([FromQuery] string lesson_id, [FromBody] LessonCaseRequest request)
    {
        var lesson = _context.LessonCases.FirstOrDefault(l => l.ID == lesson_id);

        if (lesson == null)
        {
            return NotFound(new { message = $"Lesson not found."});
        }
        var current = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        var lessonCase = new LessonCase
        {
            ID = Guid.NewGuid().ToString(),
            LessonID = lesson_id,
            Type = request.Type,
            StudentID = request.StudentID,
            Description = request.Description,
            CreatedAt = current,
            OldDate = request.OldDate,
            NewDate = request.NewDate
        };

        _context.LessonCases.Add(lessonCase);
        _context.SaveChanges();

        return Ok(new { message = "Lesson case added successfully", case_id = lessonCase.ID });
    }

    [HttpPost("upload-receipt")]
    public async Task<IActionResult> UploadReceipt([FromQuery] string lesson_case_id, IFormFile file)
    {
        var lessonCase = _context.LessonCases.FirstOrDefault(lc => lc.ID == lesson_case_id);
        if (lessonCase == null)
        {
            return NotFound(new { message = "Lesson case not found" });
        }

        var uploadPath = Path.Combine("uploads", "receipts");
        if (!Directory.Exists(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
        }

        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(uploadPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var receipt = new LessonCaseFile
        {
            LessonCaseID = lesson_case_id,
            FilePath = filePath
        };

        _context.LessonCaseFiles.Add(receipt);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Receipt uploaded successfully", file_id = receipt.ID });
    }

    [HttpGet("download-receipt")]
    public IActionResult DownloadReceipt([FromQuery] string lesson_case_id)
    {
        var receipt = _context.LessonCaseFiles.FirstOrDefault(r => r.LessonCaseID == lesson_case_id);
        if (receipt == null)
        {
            return NotFound(new { message = "Receipt not found" });
        }

        var filePath = receipt.FilePath;
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { message = "File not found on server" });
        }

        var fileBytes = System.IO.File.ReadAllBytes(filePath);
        var fileName = Path.GetFileName(filePath);
        return File(fileBytes, "application/octet-stream", fileName);
    }
}

public class LessonCaseRequest
{
    public string Type { get; set; }
    
    [JsonPropertyName("student_id")]
    public string StudentID { get; set; }
    public string Description { get; set; }
    public long CreatedAt { get; set; }
    public long? OldDate { get; set; }
    public long? NewDate { get; set; }
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
    public List<LessonCaseResponse> Cases { get; set; }
}

public class LessonCaseResponse
{
    public string Type { get; set; }
    public string UserID { get; set; }
    public long Date { get; set; }
    public string Description { get; set; }
    public long CreatedAt { get; set; }
    public long? OldDate { get; set; }
    public long? NewDate { get; set; }
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
    public string Platform { get; set; }
    [JsonPropertyName("teacher_id")]
    public string TeacherID { get; set; }
    [JsonPropertyName("students_included")]
    public List<string> StudentsIncluded { get; set; }
    [JsonPropertyName("students_excluded")]
    public List<string> StudentsExcluded { get; set; }
}