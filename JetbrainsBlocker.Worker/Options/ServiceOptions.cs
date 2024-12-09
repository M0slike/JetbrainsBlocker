using System.Text.Json.Serialization;

namespace JetbrainsBlocker.Worker.Options;

public class ServiceOptions
{
    [JsonIgnore] public const string SectionName = "Service";
    [JsonIgnore] public TimeSpan Timeout => TimeSpan.FromSeconds(TimeoutInSeconds);

    public required bool JetbrainsToolboxInstalled { get; init; }
    public required int TimeoutInSeconds { get; init; }
    public required IReadOnlyList<string> ManuallyInstalledExecutables { get; init; } = new List<string>();
    public required IReadOnlyList<string> UrlBlocklist { get; init; } = new List<string>();
}
