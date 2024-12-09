using System.Runtime.Versioning;
using System.Text.Json;
using JetbrainsBlocker.Worker.Options;
using Microsoft.Win32;

namespace JetbrainsBlocker.Worker.Config;

public class ConfigManager
{
    private const string AppConfigFolder = "jetbrains-blocker";
    private const string AppConfigFile = "appsettings.json";
    private readonly string _configPath;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    public readonly string ConfigFilePath;

    public ConfigManager()
    {
        var programDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        _configPath = Path.Join(programDataFolder, AppConfigFolder);
        ConfigFilePath = Path.Join(_configPath, AppConfigFile);

        _jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.General) { WriteIndented = true };
    }

    public bool IsConfigFileExist()
    {
        return File.Exists(ConfigFilePath);
    }

    [SupportedOSPlatform("windows")]
    public async Task WriteDefaultsAsync(CancellationToken ct = new())
    {
        var defaultConfig = new Options.Config
        {
            Serilog = new SerilogOptions { MinimumLevel = "Information" },
            Service = new ServiceOptions
            {
                JetbrainsToolboxInstalled = ToolboxInstalled(),
                UrlBlocklist = new List<string> { "account.jetbrains.com" },
                ManuallyInstalledExecutables = new List<string>(),
                TimeoutInSeconds = 300
            }
        };

        Directory.CreateDirectory(_configPath);

        await using var stream = File.Create(ConfigFilePath);
        await JsonSerializer.SerializeAsync(stream, defaultConfig, _jsonSerializerOptions, ct);
        await stream.FlushAsync(ct);
    }

    [SupportedOSPlatform("windows")]
    private static bool ToolboxInstalled()
    {
        const string RegKey = @"Software\Jetbrains\Toolbox";

        using var a = Registry.CurrentUser.OpenSubKey(RegKey);
        return a is not null;
    }
}
