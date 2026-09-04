using CloudOps.Domain.Common;
using CloudOps.Domain.Enums;

namespace CloudOps.Domain.Entities;

public sealed class Project : BaseEntity
{
    public string Name { get; private set; } = null!;
    public string ProjectCode { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ProjectStatus Status { get; private set; } = ProjectStatus.Planning;
    public ProjectPriority Priority { get; private set; } = ProjectPriority.Medium;
    public DateTime? StartDateUtc { get; private set; }
    public DateTime? DueDateUtc { get; private set; }
    public string CreatedById { get; private set; } = null!;
    public string? ProjectManagerId { get; private set; }
    public string OwnerId { get; private set; } = null!;
    public ICollection<ProjectTask> Tasks { get; } = new List<ProjectTask>();
    public ICollection<ProjectMember> Members { get; } = new List<ProjectMember>();

    private Project() { }

    public Project(string name, string? description, string createdById, string? projectCode = null, ProjectStatus status = ProjectStatus.Planning, ProjectPriority priority = ProjectPriority.Medium, DateTime? startDateUtc = null, DateTime? dueDateUtc = null, string? projectManagerId = null)
    {
        Rename(name);
        Description = description?.Trim();
        CreatedById = createdById;
        OwnerId = string.IsNullOrWhiteSpace(projectManagerId) ? createdById : projectManagerId.Trim();
        ProjectManagerId = OwnerId;
        ProjectCode = string.IsNullOrWhiteSpace(projectCode) ? GenerateProjectCode(name) : projectCode.Trim();
        Status = status;
        Priority = priority;
        StartDateUtc = startDateUtc;
        DueDateUtc = dueDateUtc;
        if (dueDateUtc is not null && startDateUtc is not null && dueDateUtc < startDateUtc) throw new ArgumentException("DueDate cannot be earlier than StartDate.", nameof(dueDateUtc));
    }

    public void Update(string name, string? description, string? projectCode = null, ProjectStatus? status = null, ProjectPriority? priority = null, DateTime? startDateUtc = null, DateTime? dueDateUtc = null, string? projectManagerId = null)
    {
        Rename(name);
        Description = description?.Trim();
        if (!string.IsNullOrWhiteSpace(projectCode)) ProjectCode = projectCode.Trim();
        if (status is not null) Status = status.Value;
        if (priority is not null) Priority = priority.Value;
        if (startDateUtc is not null) StartDateUtc = startDateUtc.Value;
        if (dueDateUtc is not null) DueDateUtc = dueDateUtc.Value;
        if (StartDateUtc is not null && DueDateUtc is not null && DueDateUtc < StartDateUtc) throw new ArgumentException("DueDate cannot be earlier than StartDate.", nameof(dueDateUtc));
        if (!string.IsNullOrWhiteSpace(projectManagerId))
        {
            ProjectManagerId = projectManagerId.Trim();
            OwnerId = ProjectManagerId;
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AddMember(ProjectMember member)
    {
        if (member is null) throw new ArgumentNullException(nameof(member));
        if (Members.Any(existing => existing.UserId == member.UserId)) throw new InvalidOperationException("User is already a member of this project.");
        Members.Add(member);
    }

    private void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Project name is required.", nameof(name));
        Name = name.Trim();
        if (string.IsNullOrWhiteSpace(ProjectCode)) ProjectCode = GenerateProjectCode(Name);
    }

    private static string GenerateProjectCode(string name)
    {
        var clean = new string(name.Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrWhiteSpace(clean)) return "PRJ";
        return clean[..Math.Min(12, clean.Length)].ToUpperInvariant();
    }
}
