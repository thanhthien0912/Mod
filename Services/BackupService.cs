using GtavOfflineModLauncher.Helpers;

namespace GtavOfflineModLauncher.Services;

public sealed class BackupService
{
    public string BackupFile(string sourceFilePath, string backupRootPath, string backupNamePrefix)
    {
        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("Backup source file was not found.", sourceFilePath);
        }

        PathHelper.EnsureDirectory(backupRootPath);

        var extension = Path.GetExtension(sourceFilePath);
        var fileName = $"{backupNamePrefix}_{DateTime.Now:yyyyMMdd_HHmmss_fff}{extension}";
        var backupPath = Path.Combine(backupRootPath, fileName);

        File.Copy(sourceFilePath, backupPath, overwrite: false);
        return backupPath;
    }
}
