using Basecamp_Backend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Basecamp_Backend.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<Discussion> Discussions { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectMember> ProjectMembers { get; set; }
        public DbSet<ProjectTask> ProjectTasks { get; set; }
        public DbSet<ProjectThread> ProjectThreads { get; set; }
        public DbSet<ThreadMessage> ThreadMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ProjectTask>().ToTable("projectTasks");

            builder.Entity<ProjectMember>()
                .HasIndex(m => new { m.ProjectId, m.AppUserId })
                .IsUnique();

            builder.Entity<ProjectMember>()
                .HasOne(m => m.Project)
                .WithMany(p => p.Members)
                .HasForeignKey(m => m.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProjectMember>()
                .HasOne(m => m.AppUser)
                .WithMany(u => u.ProjectMembers)
                .HasForeignKey(m => m.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Attachment>()
                .HasOne(a => a.Project)
                .WithMany(p => p.Attachments)
                .HasForeignKey(a => a.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Attachment>()
                .HasOne(a => a.UploadedByUser)
                .WithMany(u => u.UploadedAttachments)
                .HasForeignKey(a => a.UploadedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<ProjectThread>()
                .HasOne(t => t.Project)
                .WithMany(p => p.Threads)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProjectThread>()
                .HasOne(t => t.CreatedByUser)
                .WithMany(u => u.CreatedThreads)
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<ThreadMessage>()
                .HasOne(m => m.ProjectThread)
                .WithMany(t => t.Messages)
                .HasForeignKey(m => m.ProjectThreadId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ThreadMessage>()
                .HasOne(m => m.User)
                .WithMany(u => u.ThreadMessages)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
