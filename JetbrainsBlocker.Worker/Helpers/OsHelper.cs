using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using static System.Security.Principal.WindowsIdentity;

namespace JetbrainsBlocker.Worker.Helpers;

internal static class OsHelper
{
    [SupportedOSPlatform("windows")]
    public static bool HasAdminRole()
    {
        using var identity = GetCurrent();
        var principal = new WindowsPrincipal(identity);

        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static bool IsHostOsWindows()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    [SupportedOSPlatform("windows")]
    public static string? ResolveLnkToTargetFile(string lnkPath)
    {
        var lnk = Lnk.Lnk.LoadFile(lnkPath);

        var result = lnk.LocalPath;
        if (File.Exists(result))
        {
            return result;
        }

        result = Path.GetFullPath(lnk.RelativePath);
        if (File.Exists(result))
        {
            return result;
        }

        result = Path.Combine("C:\\Users", lnk.CommonPath);
        return File.Exists(result) ? result : null;
    }

    public static List<string> GetShortcutsInFolder(string srcFolderPath)
    {
        var srcFiles = Directory.GetFiles(srcFolderPath);

        return (from file in srcFiles
                where Path.GetExtension(file).Equals(".lnk", StringComparison.CurrentCultureIgnoreCase)
                select ResolveLnkToTargetFile(file)
                into targetPath
                where !string.IsNullOrWhiteSpace(targetPath)
                select targetPath)
            .ToList();
    }
}
