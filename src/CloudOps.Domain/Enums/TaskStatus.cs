using System.Text.Json.Serialization;

namespace CloudOps.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TaskStatus
{
    ToDo = 0,
    InProgress = 1,
    Blocked = 2,
    Completed = 3,
    InReview = 4,
    Done = 5
}
