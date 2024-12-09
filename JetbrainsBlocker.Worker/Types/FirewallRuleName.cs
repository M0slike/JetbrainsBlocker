namespace JetbrainsBlocker.Worker.Types;

internal sealed class FirewallRuleName(string filePath)
{
    public const string Prefix = "JetbrainsBlocker";

    private string AppName { get; } = new FileInfo(filePath).Name;


    public static implicit operator string(FirewallRuleName f)
    {
        return f.ToString();
    }

    public override string ToString()
    {
        return $"{Prefix} - {AppName}";
    }
}
