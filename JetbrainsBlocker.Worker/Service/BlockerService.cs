using JetbrainsBlocker.Worker.Helpers;
using JetbrainsBlocker.Worker.Options;
using Microsoft.Extensions.Options;
using WindowsFirewallHelper.Addresses;

namespace JetbrainsBlocker.Worker.Service;

internal sealed class BlockerService(
    ILogger<BlockerService> logger,
    IOptionsMonitor<ServiceOptions> optionsMonitor,
    FirewallService firewallService)
{
    public async Task BlockAsync(CancellationToken ct = new())
    {
        var toolboxExeList = GetToolboxExeList(optionsMonitor.CurrentValue.JetbrainsToolboxInstalled);
        var configExeList = optionsMonitor.CurrentValue.ManuallyInstalledExecutables;

        if (toolboxExeList.Count != 0)
        {
            var listStr = Environment.NewLine + string.Join(Environment.NewLine, toolboxExeList);
            logger.LogInformation(
                "Found {ExeCount} executables to block installed via Jetbrains Toolbox app:{ExeList}",
                toolboxExeList.Count, listStr);
        }
        
        if (configExeList.Count != 0)
        {
            var listStr = Environment.NewLine + string.Join(Environment.NewLine, configExeList);
            logger.LogInformation(
                "Found {ExeCount} executables to block from config:{ExeList}",
                configExeList.Count, listStr);
        }

        var executablesToBlock = new List<string>()
            .Concat(toolboxExeList)
            .Concat(configExeList)
            .Distinct()
            .ToList();

        if (executablesToBlock.Count == 0)
        {
            if (toolboxExeList.Count == 0)
            {
                logger.LogWarning(
                    "No executables to block: Jetbrains Toolbox path not found or no application installed");
            }

            if (configExeList.Count == 0)
            {
                logger.LogWarning(
                    "No executables to block: config section \"Service.ManuallyInstalledExecutables\" is empty or not defined");
            }

            return;
        }

        if (optionsMonitor.CurrentValue.UrlBlocklist.Count == 0)
        {
            logger.LogWarning("No urls to block: config section \"Service.UrlBlocklist\" is empty or not defined");

            return;
        }

        var resolvedIpList = new List<SingleIP>(optionsMonitor.CurrentValue.UrlBlocklist.Count);
        foreach (var url in optionsMonitor.CurrentValue.UrlBlocklist)
        {
            var entries = await DnsHelper.LookupUrlIpAddressListAsync(url, ct);

            logger.LogDebug("Url {Url} resolved to: {IpList}", url, entries);

            resolvedIpList.AddRange(entries);
        }

        logger.LogInformation("Removing previously created rules");
        firewallService.ClearBlockerRules();

        logger.LogInformation("Creating new rules");
        
        foreach (var exe in executablesToBlock)
        {
            try
            {
                firewallService.CreateRule(exe, resolvedIpList);
            }
            catch (Exception e)
            {
                logger.LogError(e, "failed to create firewall rule: {Message}", e.Message);
            }
        }
        
        logger.LogInformation("Rules created");
    }

    private static string? GetToolboxStartMenuPath()
    {
        var programsFolder =
            Environment.GetFolderPath(Environment.SpecialFolder.Programs, Environment.SpecialFolderOption.None);

        if (programsFolder == "")
        {
            return null;
        }

        var toolboxFolder = Path.Join(programsFolder, "JetBrains Toolbox");

        return !Directory.Exists(toolboxFolder) ? null : toolboxFolder;
    }

    private static IReadOnlyList<string> GetToolboxExeList(bool isToolboxInstalled)
    {
        if (!isToolboxInstalled)
        {
            return Array.Empty<string>();
        }

        var toolboxFolder = GetToolboxStartMenuPath();
        if (toolboxFolder is null)
        {
            return Array.Empty<string>();
        }

        var shortcuts = Directory.GetFiles(toolboxFolder);
        if (shortcuts.Length == 0)
        {
            return Array.Empty<string>();
        }

        return (from lnk in shortcuts
            where Path.GetFileNameWithoutExtension(lnk) != "JetBrains Toolbox"
            where Path.GetExtension(lnk) == ".lnk"
            select OsHelper.ResolveLnkToTargetFile(lnk)
            into exePath
            where File.Exists(exePath) && Path.GetExtension(exePath) == ".exe"
            select exePath).ToList();
    }
}
