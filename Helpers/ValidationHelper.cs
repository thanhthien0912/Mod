using System.Text.RegularExpressions;

namespace GtavOfflineModLauncher.Helpers;

public static class ValidationHelper
{
    private static readonly Regex SafeModNameRegex = new("^[A-Za-z0-9_-]+$", RegexOptions.Compiled);

    public static bool TryValidateGtaPath(string? gtaPath, out string error)
    {
        if (string.IsNullOrWhiteSpace(gtaPath))
        {
            error = "GTA V folder path is required.";
            return false;
        }

        if (!Directory.Exists(gtaPath))
        {
            error = "The selected GTA V folder does not exist.";
            return false;
        }

        var exePath = Path.Combine(gtaPath, "GTA5.exe");
        if (!File.Exists(exePath))
        {
            error = "Invalid GTA V folder. GTA5.exe was not found.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool TryValidateDlcRpfPath(string? dlcRpfPath, out string error)
    {
        if (string.IsNullOrWhiteSpace(dlcRpfPath))
        {
            error = "Please select a dlc.rpf file.";
            return false;
        }

        if (!File.Exists(dlcRpfPath))
        {
            error = "The selected dlc.rpf file does not exist.";
            return false;
        }

        if (!string.Equals(Path.GetFileName(dlcRpfPath), "dlc.rpf", StringComparison.OrdinalIgnoreCase))
        {
            error = "Only a file named dlc.rpf is supported.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool TryValidateModName(string? modName, out string error)
    {
        if (string.IsNullOrWhiteSpace(modName))
        {
            error = "Mod folder name is required.";
            return false;
        }

        if (!SafeModNameRegex.IsMatch(modName))
        {
            error = "Mod folder name only allows letters, numbers, underscore, and hyphen.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static string SanitizeSpawnName(string? spawnName, string fallbackModName)
    {
        return string.IsNullOrWhiteSpace(spawnName) ? fallbackModName : spawnName.Trim();
    }
}
