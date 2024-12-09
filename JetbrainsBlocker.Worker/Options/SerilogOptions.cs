using System.Text.Json.Serialization;

namespace JetbrainsBlocker.Worker.Options;

public class SerilogOptions
{
    [JsonIgnore] public const string SectionName = "Service";

    public string MinimumLevel { get; init; } = "Information";
}
