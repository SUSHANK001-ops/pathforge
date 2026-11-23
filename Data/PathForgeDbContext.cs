using Microsoft.EntityFrameworkCore;
using PathForge.Models.Entities;

namespace PathForge.Data
{
    public class PathForgeDbContext : DbContext
    {
        public PathForgeDbContext(DbContextOptions<PathForgeDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<CourseEnrolment> CourseEnrolments { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<AssignmentSubmission> AssignmentSubmissions { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<QuizQuestion> QuizQuestions { get; set; }
        public DbSet<QuizOption> QuizOptions { get; set; }
        public DbSet<QuizAttempt> QuizAttempts { get; set; }
        public DbSet<StudentProgress> StudentProgress { get; set; }
        public DbSet<DiscussionPost> DiscussionPosts { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<VirtualLab> VirtualLabs { get; set; }
        public DbSet<VirtualLabSubmission> VirtualLabSubmissions { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<Certificate> Certificates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Prevent cascade delete loops on self-referencing DiscussionPost
            modelBuilder.Entity<DiscussionPost>()
                .HasOne(p => p.ParentPost)
                .WithMany(p => p.Replies)
                .HasForeignKey(p => p.ParentPostId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent double cascade on ChatMessage (Sender + Receiver both FK to User)
            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique email constraint
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // One virtual lab per module
            modelBuilder.Entity<VirtualLab>()
                .HasIndex(l => l.ModuleId)
                .IsUnique();

            // A student has at most one submission per lab
            modelBuilder.Entity<VirtualLabSubmission>()
                .HasIndex(s => new { s.VirtualLabId, s.StudentId })
                .IsUnique();

            // Don't cascade-delete the student when a submission is removed
            modelBuilder.Entity<VirtualLabSubmission>()
                .HasOne(s => s.Student)
                .WithMany()
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // SQL Server rejects multiple cascade paths to the same table.
            // Each of these has a StudentId FK to Users AND a second cascade
            // path to Users via Course/Assignment/Quiz. Break the Users path
            // with Restrict so only the Course-side cascade applies.
            modelBuilder.Entity<CourseEnrolment>()
                .HasOne(e => e.Student)
                .WithMany(u => u.Enrolments)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AssignmentSubmission>()
                .HasOne(s => s.Student)
                .WithMany()
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<QuizAttempt>()
                .HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentProgress>()
                .HasOne(p => p.Student)
                .WithMany(u => u.ProgressRecords)
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DiscussionPost>()
                .HasOne(p => p.Author)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Announcement: Course cascade is fine; break the Users path.
            modelBuilder.Entity<Announcement>()
                .HasOne(a => a.Author)
                .WithMany()
                .HasForeignKey(a => a.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Certificate: one per student per course; break the Users path.
            modelBuilder.Entity<Certificate>()
                .HasOne(c => c.Student)
                .WithMany()
                .HasForeignKey(c => c.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Certificate>()
                .HasIndex(c => new { c.StudentId, c.CourseId })
                .IsUnique();
        }
    }
}
