namespace CloudOps.Application.Abstractions;

public interface ICurrentUser
{
    string? Id { get; }
    bool IsInRole(string role);
}
