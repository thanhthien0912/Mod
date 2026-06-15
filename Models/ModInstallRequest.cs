namespace GtavOfflineModLauncher.Models;

public sealed class ModInstallRequest
{
    public string GtaPath { get; set; } = string.Empty;
    public string SourceDlcRpfPath { get; set; } = string.Empty;
    public string ModName { get; set; } = string.Empty;
    public string SpawnName { get; set; } = string.Empty;
    public bool AutoEditDlclist { get; set; }
}
