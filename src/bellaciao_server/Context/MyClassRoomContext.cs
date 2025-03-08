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
                entity.Property(e => e.NewDate).HasColumnName("new_date");
                entity.Property(e => e.OldDate).HasColumnName("old_date");
                entity.Property(e => e.Description).HasColumnName("description");
            });

            modelBuilder.Entity<LessonCaseFile>(entity =>
            {
                entity.ToTable("lesson_case_files");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.ID).HasColumnName("id");
                entity.Property(e => e.LessonCaseID).HasColumnName("lesson_case_id");
                entity.Property(e => e.FilePath).HasColumnName("file_path");
            });
        }
    }

    public class User
    {
        public string ID { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        // public string Role { get; set; } // Uncomment if Role is needed
    }

    public class Classroom
    {
        public string ID { get; set; }
        public string Title { get; set; }
        public string HeadID { get; set; }
        // public List<User> Participants { get; set; } // Uncomment if Participants is needed
    }


    public class Invitation
    {
        public string ID { get; set; }
        public string Role { get; set; }
        public string ClassroomID { get; set; }
    }

    public class ClassUser
    {
        public string ClassroomID { get; set; }
        public string UserID { get; set; }
        public string Role { get; set; }
        public decimal Charge { get; set; }
    }
}
