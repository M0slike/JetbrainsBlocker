namespace JetbrainsBlocker.Worker.Options;

public class Config
{
    public required SerilogOptions Serilog { get; init; }
    public required ServiceOptions Service { get; init; }
}
