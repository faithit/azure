namespace CloudOps.Application.Common;

public sealed class NotFoundException(string resource, object key) : Exception($"{resource} with identifier '{key}' was not found.");
public sealed class ForbiddenException(string message = "You do not have permission to perform this action.") : Exception(message);
public sealed class ConflictException(string message) : Exception(message);
