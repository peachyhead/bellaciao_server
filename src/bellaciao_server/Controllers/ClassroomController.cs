using Microsoft.AspNetCore.Mvc;
using Context;

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

    [HttpPost("create-classroom")]
    public ActionResult CreateClassroom([FromBody] Classroom classroom)
    {
        // Добавляем новую комнату в базу
        _context.Classrooms.Add(classroom);
        _context.SaveChanges();

        // Создаем запись в class_users, связывая пользователя head_id с классом
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

    [HttpPost("add-user")]
    public ActionResult AddUser([FromBody] User user)
    {
        _context.Users.Add(user);
        _context.SaveChanges();
        return CreatedAtAction(nameof(GetUserById), new { user.ID }, user);
    }

    [HttpGet("get-user")]
    public ActionResult<User> GetUserById([FromQuery] string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest("ID is required.");
        }

        var user = _context.Users.Find(id);
        if (user == null)
        {
            return NotFound();
        }
        return Ok(user);
    }

    [HttpPost("invite")]
    public ActionResult<InviteResponse> InviteUser([FromBody] InviteRequest request)
    {
        if (string.IsNullOrEmpty(request.role) || string.IsNullOrEmpty(request.classroom_id))
        {
            return BadRequest("Role and Classroom ID are required.");
        }

        // Генерируем уникальный ID для приглашения
        string invitationId = Guid.NewGuid().ToString();

        // Здесь можно сохранить приглашение в базу данных, если нужно
        var invite = new Invitation
        {
            ID = invitationId,
            Role = request.role,
            ClassroomID = request.classroom_id
        };

        _context.Invitations.Add(invite);
        _context.SaveChanges();

        // Возвращаем ID приглашения
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

        // Проверяем, есть ли уже такой пользователь в классе
        bool userExists = _context.ClassUsers.Any(cu => cu.ClassroomID == invitation.ClassroomID && cu.UserID == request.user_id);
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

        // Находим все классы, в которых состоит пользователь
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

        // Получаем список пользователей по роли и ID комнаты
        var userIds = _context.ClassUsers
            .Where(cu => cu.ClassroomID == room_id && cu.Role == role)
            .Select(cu => cu.UserID)
            .ToList();

        return Ok(new { users = userIds });
    }

    // Класс для ответа
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
}