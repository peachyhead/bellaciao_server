using Context;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/classroom/{room_id}/lesson/{lesson_id}/case")]
public class LessonCaseController : ControllerBase
{
    private readonly MyClassroomContext _context;

    public LessonCaseController(MyClassroomContext context)
    {
        _context = context;
    }

    [HttpPost("add")]
    public ActionResult AddLessonCase(string lesson_id, [FromBody] LessonCaseRequest request)
    {
        var lesson = _context.Lessons.FirstOrDefault(l => l.ID == lesson_id);
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
            LessonDate = request.LessonDate,
            PeriodicType = request.PeriodicType,
            PeriodicTime = request.PeriodicTime
        };

        _context.LessonCases.Add(lessonCase);
        _context.SaveChanges();

        return Ok(new { message = "Lesson case added successfully", case_id = lessonCase.ID });
    }

    [HttpPost("{id}/upload-file")]
    public async Task<IActionResult> UploadReceipt(string id, IFormFile file)
    {
        var lessonCase = _context.LessonCases.FirstOrDefault(lc => lc.ID == id);
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
            LessonCaseID = id,
            FilePath = filePath
        };

        _context.LessonCaseFiles.Add(receipt);
        await _context.SaveChangesAsync();

        return Ok(new { message = "File uploaded successfully", file_id = receipt.ID });
    }

    [HttpGet("{id}/download-file")]
    public IActionResult DownloadReceipt(string id)
    {
        var receipt = _context.LessonCaseFiles.FirstOrDefault(r => r.LessonCaseID == id);
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