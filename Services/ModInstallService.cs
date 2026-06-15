using System.Diagnostics;
using GtavOfflineModLauncher.Helpers;
using GtavOfflineModLauncher.Models;

namespace GtavOfflineModLauncher.Services;

public sealed class ModInstallService
{
    private const string DlclistInternalPath = "common/data/dlclist.xml";

    private readonly BackupService _backupService;
    private readonly DlcListService _dlcListService;
    private readonly IRpfService _rpfService;
    private readonly JsonStorageService _jsonStorageService;

    public ModInstallService(
        BackupService backupService,
        DlcListService dlcListService,
        IRpfService rpfService,
        JsonStorageService jsonStorageService)
    {
        _backupService = backupService;
        _dlcListService = dlcListService;
        _rpfService = rpfService;
        _jsonStorageService = jsonStorageService;
    }

    public async Task<InstalledMod> InstallModAsync(ModInstallRequest request, Action<string> log)
    {
        ValidateRequest(request);

        var modName = request.ModName.Trim();
        var spawnName = ValidationHelper.SanitizeSpawnName(request.SpawnName, modName);
        var backupRootPath = PathHelper.GetBackupRootPath(request.GtaPath);
        var targetFolderPath = PathHelper.GetModFolderPath(request.GtaPath, modName);
        var targetDlcRpfPath = PathHelper.GetModDlcRpfPath(request.GtaPath, modName);
        var modsUpdateRpfPath = PathHelper.GetModsUpdateRpfPath(request.GtaPath);
        var originalUpdateRpfPath = PathHelper.GetOriginalUpdateRpfPath(request.GtaPath);
        var dlclistItemXml = $"<Item>{_dlcListService.BuildEntry(modName)}</Item>";

        log($"Installing mod '{modName}'...");

        // Always install into the mods folder only. Never touch the original game archives directly.
        PathHelper.EnsureDirectory(targetFolderPath);
        log($"Ensured mod folder: {targetFolderPath}");

        if (File.Exists(targetDlcRpfPath))
        {
            var oldDlcBackupPath = _backupService.BackupFile(targetDlcRpfPath, backupRootPath, $"{modName}_existing_dlc");
            log($"Backed up existing dlc.rpf: {oldDlcBackupPath}");
        }

        File.Copy(request.SourceDlcRpfPath, targetDlcRpfPath, overwrite: true);
        log($"Copied dlc.rpf to: {targetDlcRpfPath}");

        EnsureModsUpdateRpfExists(originalUpdateRpfPath, modsUpdateRpfPath, log);

        // Backup mods\update\update.rpf before any future RPF edit attempt.
        var updateRpfBackupPath = _backupService.BackupFile(modsUpdateRpfPath, backupRootPath, "update_rpf");
        log($"Backed up mods update.rpf: {updateRpfBackupPath}");
        log($"Generated dlclist entry: {dlclistItemXml}");

        if (request.AutoEditDlclist)
        {
            // Current MVP uses a stub RPF service. The launcher must warn clearly instead of crashing.
            TryAutoEditDlclist(modsUpdateRpfPath, modName, log);
        }
        else
        {
            log("Auto edit is disabled. Please add the dlclist entry manually in OpenIV.");
        }

        var installedMod = new InstalledMod
        {
            Name = modName,
            SpawnName = spawnName,
            InstalledAt = DateTime.Now,
            DlcRpfPath = targetDlcRpfPath,
            DlclistEntry = dlclistItemXml,
            Enabled = true
        };

        var installedModsFile = await _jsonStorageService.LoadInstalledModsAsync();
        installedModsFile.Mods.RemoveAll(x => string.Equals(x.Name, modName, StringComparison.OrdinalIgnoreCase));
        installedModsFile.Mods.Add(installedMod);
        await _jsonStorageService.SaveInstalledModsAsync(installedModsFile);

        log($"Installed mod {modName}");
        log($"Spawn name: {spawnName}");
        log($"Path: {targetDlcRpfPath}");
        log($"dlclist entry: {dlclistItemXml}");

        return installedMod;
    }

    public async Task UninstallModAsync(string gtaPath, InstalledMod mod, bool autoEditDlclist, Action<string> log)
    {
        if (!ValidationHelper.TryValidateGtaPath(gtaPath, out var gtaError))
        {
            throw new InvalidOperationException(gtaError);
        }

        if (mod is null)
        {
            throw new ArgumentNullException(nameof(mod));
        }

        var modFolderPath = PathHelper.GetModFolderPath(gtaPath, mod.Name);
        log($"Uninstalling mod '{mod.Name}'...");

        if (Directory.Exists(modFolderPath))
        {
            Directory.Delete(modFolderPath, recursive: true);
            log($"Deleted mod folder: {modFolderPath}");
        }
        else
        {
            log($"Mod folder not found, skipping delete: {modFolderPath}");
        }

        var modsUpdateRpfPath = PathHelper.GetModsUpdateRpfPath(gtaPath);
        if (autoEditDlclist && File.Exists(modsUpdateRpfPath))
        {
            var backupRootPath = PathHelper.GetBackupRootPath(gtaPath);
            var updateRpfBackupPath = _backupService.BackupFile(modsUpdateRpfPath, backupRootPath, "update_rpf_uninstall");
            log($"Backed up mods update.rpf before dlclist cleanup: {updateRpfBackupPath}");

            try
            {
                var xmlContent = _rpfService.ExtractTextFile(modsUpdateRpfPath, DlclistInternalPath);
                var updatedXml = _dlcListService.RemoveDlcPackEntry(xmlContent, mod.Name);
                _rpfService.ReplaceTextFile(modsUpdateRpfPath, DlclistInternalPath, updatedXml);
                log($"Removed dlclist entry for '{mod.Name}' from update.rpf.");
            }
            catch (NotImplementedException ex)
            {
                log(ex.Message);
                log($"Please remove this line manually in OpenIV: {mod.DlclistEntry}");
            }
        }
        else
        {
            log($"RPF auto edit is not active. Please remove this line manually in OpenIV: {mod.DlclistEntry}");
        }

        var installedModsFile = await _jsonStorageService.LoadInstalledModsAsync();
        installedModsFile.Mods.RemoveAll(x => string.Equals(x.Name, mod.Name, StringComparison.OrdinalIgnoreCase));
        await _jsonStorageService.SaveInstalledModsAsync(installedModsFile);
        log($"Uninstalled mod '{mod.Name}'.");
    }

    public void LaunchGtaOffline(string gtaPath, Action<string> log)
    {
        if (!ValidationHelper.TryValidateGtaPath(gtaPath, out var error))
        {
            throw new InvalidOperationException(error);
        }

        var exePath = Path.Combine(gtaPath, "GTA5.exe");
        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException("GTA5.exe was not found.", exePath);
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = gtaPath,
            UseShellExecute = true
        });

        log("Launched GTA V Offline / Story Mode.");
    }

    private static void ValidateRequest(ModInstallRequest request)
    {
        if (!ValidationHelper.TryValidateGtaPath(request.GtaPath, out var gtaError))
        {
            throw new InvalidOperationException(gtaError);
        }

        if (!ValidationHelper.TryValidateDlcRpfPath(request.SourceDlcRpfPath, out var dlcError))
        {
            throw new InvalidOperationException(dlcError);
        }

        if (!ValidationHelper.TryValidateModName(request.ModName, out var modError))
        {
            throw new InvalidOperationException(modError);
        }
    }

    private static void EnsureModsUpdateRpfExists(string originalUpdateRpfPath, string modsUpdateRpfPath, Action<string> log)
    {
        if (File.Exists(modsUpdateRpfPath))
        {
            log($"Found mods update.rpf: {modsUpdateRpfPath}");
            return;
        }

        // Prepare the modded archive by copying from the original game file once.

        if (!File.Exists(originalUpdateRpfPath))
        {
            throw new FileNotFoundException("Original update.rpf was not found. Cannot prepare mods/update/update.rpf.", originalUpdateRpfPath);
        }

        var directory = Path.GetDirectoryName(modsUpdateRpfPath)
            ?? throw new InvalidOperationException("Invalid mods update.rpf path.");

        Directory.CreateDirectory(directory);
        File.Copy(originalUpdateRpfPath, modsUpdateRpfPath, overwrite: false);
        log($"Copied original update.rpf to mods folder: {modsUpdateRpfPath}");
    }

    private void TryAutoEditDlclist(string modsUpdateRpfPath, string modName, Action<string> log)
    {
        try
        {
            // The RPF abstraction is intentionally isolated here so a real implementation can replace the stub later.
            var xmlContent = _rpfService.ExtractTextFile(modsUpdateRpfPath, DlclistInternalPath);
            var updatedXml = _dlcListService.AddDlcPackEntry(xmlContent, modName);
            _rpfService.ReplaceTextFile(modsUpdateRpfPath, DlclistInternalPath, updatedXml);
            log($"Updated dlclist.xml inside update.rpf for '{modName}'.");
        }
        catch (NotImplementedException ex)
        {
            log(ex.Message);
            log($"Please add this line manually in OpenIV: <Item>{_dlcListService.BuildEntry(modName)}</Item>");
        }
        catch (Exception ex)
        {
            log($"Auto edit dlclist failed: {ex.Message}");
            log($"Please add this line manually in OpenIV: <Item>{_dlcListService.BuildEntry(modName)}</Item>");
        }
    }
}
