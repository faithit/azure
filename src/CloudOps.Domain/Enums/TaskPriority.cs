using System.Text.Json.Serialization;

namespace CloudOps.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TaskPriority
{
    Low,
    Medium,
    High,
    Critical
}
