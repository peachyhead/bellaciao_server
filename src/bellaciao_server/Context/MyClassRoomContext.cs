using Microsoft.EntityFrameworkCore;

namespace Context
{
    public class MyClassroomContext : DbContext
    {
        public MyClassroomContext(DbContextOptions<MyClassroomContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Classroom> Classrooms { get; set; }
        public DbSet<Invitation> Invitations { get; set; }
        public DbSet<ClassUser> ClassUsers {get; set; }

        public DbSet<Chat> Chats { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ChatParticipant> ChatParticipants { get; set; }

        public DbSet<Lesson> Lessons {get; set; }
        public DbSet<LessonParticipant> LessonParticipants {get; set; }
        public DbSet<LessonCase> LessonCases {get; set; }
        public DbSet<LessonCaseFile> LessonCaseFiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.ID).HasColumnName("id");
                entity.Property(e => e.FullName).HasColumnName("fullname");
                entity.Property(e => e.Phone).HasColumnName("phone");
            });

            modelBuilder.Entity<Classroom>(entity =>
            {
                entity.ToTable("classrooms");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.ID).HasColumnName("id");
                entity.Property(e => e.Title).HasColumnName("title");
                entity.Property(e => e.HeadID).HasColumnName("head_id");
            });

            modelBuilder.Entity<Invitation>(entity =>
            {
                entity.ToTable("classroom_invites");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.ID).HasColumnName("id");
                entity.Property(e => e.Role).HasColumnName("role");
                entity.Property(e => e.ClassroomID).HasColumnName("classroom_id");
                entity.Property(e => e.Charge).HasColumnName("charge");
            });

            modelBuilder.Entity<ClassUser>(entity =>
            {
                entity.ToTable("class_users");
                entity.HasKey(e => e.ClassroomID);
                entity.Property(e => e.ClassroomID).HasColumnName("classroom_id");
                entity.Property(e => e.UserID).HasColumnName("user_id");
                entity.Property(e => e.Role).HasColumnName("role");
                entity.Property(e => e.Charge).HasColumnName("charge");
            });

            modelBuilder.Entity<Lesson>(entity =>
            {
                entity.ToTable("lessons");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.ID).HasColumnName("id");
                entity.Property(e => e.PeriodicType).HasColumnName("periodic_type");
                entity.Property(e => e.PeriodicTime).HasColumnName("periodic_time");
                entity.Property(e => e.RoomID).HasColumnName("classroom_id");
                entity.Property(e => e.Duration).HasColumnName("duration");
                entity.Property(e => e.Platform).HasColumnName("platform"); 
            });

            modelBuilder.Entity<LessonParticipant>(entity =>
            {
                entity.ToTable("lesson_participants");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.ID).HasColumnName("id");
                entity.Property(e => e.UserID).HasColumnName("user_id");
                entity.Property(e => e.LessonID).HasColumnName("lesson_id");
                entity.Property(e => e.Role).HasColumnName("role");
            });

            modelBuilder.Entity<LessonCase>(entity =>
            {
                entity.ToTable("lesson_cases");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.ID).HasColumnName("id");
                entity.Property(e => e.Type).HasColumnName("type");
                entity.Property(e => e.LessonID).HasColumnName("lesson_id");
                entity.Property(e => e.StudentID).HasColumnName("student_id");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.LessonDate).HasColumnName("lesson_date");
                entity.Property(e => e.PeriodicTime).HasColumnName("periodic_time");
                entity.Property(e => e.PeriodicType).HasColumnName("periodic_type");
                entity.Property(e => e.Description).HasColumnName("description");
            });

            modelBuilder.Entity<LessonCaseFile>(entity =>
            {
                entity.ToTable("lesson_receipts");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.ID).HasColumnName("id");
                entity.Property(e => e.LessonCaseID).HasColumnName("lesson_case_id");
                entity.Property(e => e.FilePath).HasColumnName("file_path");
            });

            modelBuilder.Entity<Chat>(entity => 
            {
                entity.ToTable("chats");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.ID).HasColumnName("id");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.LastMessage).HasColumnName("last_message");
                entity.Property(e => e.LastMessageTime).HasColumnName("last_message_time");
            });

            modelBuilder.Entity<Message>(entity => 
            {
                entity.ToTable("messages");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.ID).HasColumnName("id");
                entity.Property(e => e.Text).HasColumnName("text");
                entity.Property(e => e.ChatID).HasColumnName("chat_id");
                entity.Property(e => e.AuthorID).HasColumnName("author_id");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            modelBuilder.Entity<ChatParticipant>(entity => 
            {
                entity.ToTable("chat_participants");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.ID).HasColumnName("id");
                entity.Property(e => e.ChatID).HasColumnName("chat_id");
                entity.Property(e => e.UserID).HasColumnName("user_id");
                entity.Property(e => e.LastMessageViewed).HasColumnName("last_message_view");
            });
        }
    }

    public class Chat
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string? LastMessage { get; set; }
        public long? LastMessageTime { get; set; }
    }

    public class Message
    {
        public string ID { get; set; }
        public string ChatID { get; set; }
        public string AuthorID { get; set; }
        public string Text { get; set; }
        public long CreatedAt { get; set; }
    }

    public class ChatParticipant
    {
        public string ID { get; set; }
        public string ChatID { get; set; }
        public string UserID { get; set; }
        public string LastMessageViewed { get; set; }
    }

    public class User
    {
        public string ID { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
    }

    public class Classroom
    {
        public string ID { get; set; }
        public string Title { get; set; }
        public string HeadID { get; set; }
    }

    public class Lesson
    {
        public string ID { get; set; }
        public string RoomID { get; set; }
        public string PeriodicType { get; set; }
        public long PeriodicTime { get; set; }
        public int Duration { get; set; }
        public string Platform { get; set; }
    }

    public class LessonCase
    {
        public required string ID { get; set; }
        public required string Type { get; set; }
        public required string LessonID { get; set; }
        public required string StudentID { get; set; }
        public required long CreatedAt { get; set; }
        public required long LessonDate { get; set; }
        public string? Description { get; set; }
        public string? PeriodicType { get; set; }
        public long? PeriodicTime { get; set; }
    }

    public class LessonCaseFile
    {
        public int ID { get; set; }
        public required string LessonCaseID { get; set; }
        public required string FilePath { get; set; }
    }

    public class LessonParticipant
    {
        public int ID { get; set; }
        public string UserID { get; set; }
        public string LessonID { get; set; }
        public string Role { get; set; }
    }

    public class Invitation
    {
        public string ID { get; set; }
        public string Role { get; set; }
        public string ClassroomID { get; set; }
        public decimal Charge { get; set; }
    }

    public class ClassUser
    {
        public string ClassroomID { get; set; }
        public string UserID { get; set; }
        public string Role { get; set; }
        public decimal Charge { get; set; }
    }
}
