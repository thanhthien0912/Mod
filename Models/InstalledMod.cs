namespace GtavOfflineModLauncher.Models;

public sealed class InstalledMod
{
    public string Name { get; set; } = string.Empty;
    public string SpawnName { get; set; } = string.Empty;
    public DateTime InstalledAt { get; set; }
    public string DlcRpfPath { get; set; } = string.Empty;
    public string DlclistEntry { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;

    public string Status => Enabled ? "Enabled" : "Disabled";
}
