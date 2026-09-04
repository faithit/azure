using System.Text.Json.Serialization;

namespace CloudOps.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProjectPriority
{
    Low,
    Medium,
    High,
    Critical
}
