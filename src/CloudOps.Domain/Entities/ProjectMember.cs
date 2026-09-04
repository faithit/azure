using CloudOps.Domain.Common;

namespace CloudOps.Domain.Entities;

public sealed class ProjectMember : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public string UserId { get; private set; } = null!;
    public DateTime JoinedAtUtc { get; private set; } = DateTime.UtcNow;
    public Project Project { get; private set; } = null!;

    private ProjectMember() { }

    public ProjectMember(Guid projectId, string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User is required.", nameof(userId));
        ProjectId = projectId;
        UserId = userId.Trim();
    }
}
