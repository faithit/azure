using CloudOps.Domain.Entities;
using CloudOps.Domain.Enums;
using CloudOps.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CloudOps.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTask> Tasks => Set<ProjectTask>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Project>(entity =>
        {
            entity.ToTable("Projects");
            entity.HasKey(project => project.Id);
            entity.Property(project => project.Name).HasMaxLength(120).IsRequired();
            entity.Property(project => project.ProjectCode).HasMaxLength(30).IsRequired();
            entity.Property(project => project.Description).HasMaxLength(2000);
            entity.Property(project => project.CreatedById).HasMaxLength(450).IsRequired();
            entity.Property(project => project.OwnerId).HasMaxLength(450).IsRequired();
            entity.Property(project => project.ProjectManagerId).HasMaxLength(450);
            entity.Property(project => project.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(project => project.Priority).HasConversion<string>().HasMaxLength(30);
            entity.HasIndex(project => project.ProjectCode).IsUnique();
            entity.HasIndex(project => project.OwnerId);
            entity.HasIndex(project => project.CreatedById);
            entity.HasIndex(project => project.ProjectManagerId);
            entity.HasMany(project => project.Tasks).WithOne(task => task.Project).HasForeignKey(task => task.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(project => project.Members).WithOne(member => member.Project).HasForeignKey(member => member.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<ProjectMember>(entity =>
        {
            entity.ToTable("ProjectMembers");
            entity.HasKey(member => member.Id);
            entity.Property(member => member.UserId).HasMaxLength(450).IsRequired();
            entity.HasIndex(member => new { member.ProjectId, member.UserId }).IsUnique();
        });
        builder.Entity<ProjectTask>(entity =>
        {
            entity.ToTable("Tasks");
            entity.HasKey(task => task.Id);
            entity.Property(task => task.Title).HasMaxLength(200).IsRequired();
            entity.Property(task => task.Description).HasMaxLength(5000);
            entity.Property(task => task.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(task => task.Priority).HasConversion<string>().HasMaxLength(30);
            entity.Property(task => task.AssignedUserId).HasMaxLength(450);
            entity.Property(task => task.CreatedById).HasMaxLength(450);
            entity.HasIndex(task => new { task.ProjectId, task.Status });
        });
    }
}
