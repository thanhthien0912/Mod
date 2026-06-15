namespace GtavOfflineModLauncher.Helpers;

public static class PathHelper
{
    public static string AppSettingsPath => Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    public static string AppDataRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GTAVOfflineModLauncher");

    public static string InstalledModsPath => Path.Combine(AppDataRoot, "installed_mods.json");

    public static string GetModsRootPath(string gtaPath) => Path.Combine(gtaPath, "mods");

    public static string GetDlcpacksRootPath(string gtaPath) => Path.Combine(gtaPath, "mods", "update", "x64", "dlcpacks");

    public static string GetModFolderPath(string gtaPath, string modName) => Path.Combine(GetDlcpacksRootPath(gtaPath), modName);

    public static string GetModDlcRpfPath(string gtaPath, string modName) => Path.Combine(GetModFolderPath(gtaPath, modName), "dlc.rpf");

    public static string GetOriginalUpdateRpfPath(string gtaPath) => Path.Combine(gtaPath, "update", "update.rpf");

    public static string GetModsUpdateRpfPath(string gtaPath) => Path.Combine(gtaPath, "mods", "update", "update.rpf");

    public static string GetBackupRootPath(string gtaPath) => Path.Combine(gtaPath, "mods_launcher_backups");

    public static string EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
