using Microsoft.EntityFrameworkCore;

namespace Context
{
    public class MyClassroomContext : DbContext
    {
        public MyClassroomContext(DbContextOptions<MyClassroomContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Classroom> Classrooms { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.id);
                entity.Property(e => e.id).HasColumnName("id");
                entity.Property(e => e.fullname).HasColumnName("fullname");
                entity.Property(e => e.phone).HasColumnName("phone");
                // entity.Property(e => e.Role).HasColumnName("role"); // Uncomment if Role is needed
            });

            modelBuilder.Entity<Classroom>(entity =>
            {
                entity.ToTable("classrooms");
                entity.HasKey(e => e.id);
                entity.Property(e => e.id).HasColumnName("id");
                entity.Property(e => e.title).HasColumnName("title");
                entity.Property(e => e.head_id).HasColumnName("head_id");
                // entity.HasMany(e => e.Participants).WithOne().HasForeignKey("ClassroomId"); // Uncomment if Participants is needed
            });
        }
    }

    public class User
    {
        public string id { get; set; }
        public string fullname { get; set; }
        public string phone { get; set; }
        // public string Role { get; set; } // Uncomment if Role is needed
    }

    public class Classroom
    {
        public string id { get; set; }
        public string title { get; set; }
        public string head_id { get; set; }
        // public List<User> Participants { get; set; } // Uncomment if Participants is needed
    }
}