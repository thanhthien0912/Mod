using System.Collections.ObjectModel;
using System.Windows.Input;
using GtavOfflineModLauncher.Helpers;
using GtavOfflineModLauncher.Models;
using GtavOfflineModLauncher.Services;
using Forms = System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;
using WpfApplication = System.Windows.Application;
using WpfClipboard = System.Windows.Clipboard;
using Win32OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace GtavOfflineModLauncher.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly JsonStorageService _jsonStorageService;
    private readonly ModInstallService _modInstallService;

    private string _gtaPath = string.Empty;
    private string _selectedDlcRpfPath = string.Empty;
    private string _modName = string.Empty;
    private string _spawnName = string.Empty;
    private bool _autoEditDlclist = true;
    private bool _isBusy;
    private InstalledMod? _selectedInstalledMod;

    public MainViewModel()
    {
        _jsonStorageService = new JsonStorageService();
        _modInstallService = new ModInstallService(
            new BackupService(),
            new DlcListService(),
            new CodeWalkerRpfService(),
            _jsonStorageService);

        BrowseGtaFolderCommand = new RelayCommand(_ => BrowseGtaFolder(), _ => !IsBusy);
        SelectDlcRpfCommand = new RelayCommand(_ => SelectDlcRpf(), _ => !IsBusy);
        InstallModCommand = new RelayCommand(async _ => await InstallModAsync(), _ => !IsBusy);
        UninstallSelectedModCommand = new RelayCommand(async _ => await UninstallSelectedModAsync(), _ => !IsBusy);
        LaunchGtaOfflineCommand = new RelayCommand(_ => LaunchGtaOffline(), _ => !IsBusy);
        RefreshInstalledModsCommand = new RelayCommand(async _ => await LoadInstalledModsAsync(), _ => !IsBusy);
        InstalledMods = new ObservableCollection<InstalledMod>();
        LogEntries = new ObservableCollection<string>();
        CopyLogEntriesCommand = new RelayCommand(_ => CopyLogEntries(), _ => LogEntries.Count > 0);

        LogEntries.CollectionChanged += (_, _) => RaiseCommandStates();

        _ = InitializeAsync();
    }

    public string GtaPath
    {
        get => _gtaPath;
        set
        {
            if (SetProperty(ref _gtaPath, value))
            {
                OnPropertyChanged(nameof(TargetInstallPathPreview));
                OnPropertyChanged(nameof(BackupPathPreview));
            }
        }
    }

    public string SelectedDlcRpfPath
    {
        get => _selectedDlcRpfPath;
        set => SetProperty(ref _selectedDlcRpfPath, value);
    }

    public string ModName
    {
        get => _modName;
        set
        {
            var previous = _modName;
            if (!SetProperty(ref _modName, value))
            {
                return;
            }

            OnPropertyChanged(nameof(TargetInstallPathPreview));
            OnPropertyChanged(nameof(DlclistEntryPreview));

            if (string.IsNullOrWhiteSpace(SpawnName) || string.Equals(SpawnName, previous, StringComparison.Ordinal))
            {
                SpawnName = value;
            }
        }
    }

    public string SpawnName
    {
        get => _spawnName;
        set => SetProperty(ref _spawnName, value);
    }

    public bool AutoEditDlclist
    {
        get => _autoEditDlclist;
        set
        {
            if (SetProperty(ref _autoEditDlclist, true))
            {
                _ = SaveAppSettingsAsync();
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public InstalledMod? SelectedInstalledMod
    {
        get => _selectedInstalledMod;
        set => SetProperty(ref _selectedInstalledMod, value);
    }

    public ObservableCollection<InstalledMod> InstalledMods { get; }
    public ObservableCollection<string> LogEntries { get; }

    public string TargetInstallPathPreview => string.IsNullOrWhiteSpace(GtaPath) || string.IsNullOrWhiteSpace(ModName)
        ? "{GtaPath}\\mods\\update\\x64\\dlcpacks\\{ModName}\\dlc.rpf"
        : PathHelper.GetModDlcRpfPath(GtaPath, ModName);

    public string DlclistEntryPreview => string.IsNullOrWhiteSpace(ModName)
        ? "<Item>dlcpacks:/{ModName}/</Item>"
        : $"<Item>dlcpacks:/{ModName}/</Item>";

    public string BackupPathPreview => string.IsNullOrWhiteSpace(GtaPath)
        ? "{GtaPath}\\mods_launcher_backups"
        : PathHelper.GetBackupRootPath(GtaPath);

    public ICommand BrowseGtaFolderCommand { get; }
    public ICommand SelectDlcRpfCommand { get; }
    public ICommand InstallModCommand { get; }
    public ICommand UninstallSelectedModCommand { get; }
    public ICommand LaunchGtaOfflineCommand { get; }
    public ICommand RefreshInstalledModsCommand { get; }
    public ICommand CopyLogEntriesCommand { get; }

    private async Task InitializeAsync()
    {
        try
        {
            var settings = await _jsonStorageService.LoadAppSettingsAsync();
            GtaPath = settings.GtaPath;
            AutoEditDlclist = true;
            await SaveAppSettingsAsync();
            await LoadInstalledModsAsync();
            AddLog("Application initialized.");
        }
        catch (Exception ex)
        {
            AddLog($"Initialization failed: {ex.Message}");
        }
    }

    private void BrowseGtaFolder()
    {
        try
        {
            using var dialog = new Forms.FolderBrowserDialog
            {
                Description = "Select your GTA V offline folder",
                ShowNewFolderButton = false,
                SelectedPath = Directory.Exists(GtaPath) ? GtaPath : Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (dialog.ShowDialog() != Forms.DialogResult.OK)
            {
                return;
            }

            if (!ValidationHelper.TryValidateGtaPath(dialog.SelectedPath, out var error))
            {
                AddLog(error);
                MessageBox.Show(error, "Invalid GTA Folder", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            GtaPath = dialog.SelectedPath;
            AddLog($"Selected GTA V folder: {GtaPath}");
            _ = SaveAppSettingsAsync();
        }
        catch (Exception ex)
        {
            AddLog($"Browse GTA folder failed: {ex.Message}");
        }
    }

    private void SelectDlcRpf()
    {
        try
        {
            var dialog = new Win32OpenFileDialog
            {
                Title = "Select dlc.rpf",
                Filter = "RPF file|dlc.rpf|All files|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            if (!ValidationHelper.TryValidateDlcRpfPath(dialog.FileName, out var error))
            {
                AddLog(error);
                MessageBox.Show(error, "Invalid Mod File", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedDlcRpfPath = dialog.FileName;
            AddLog($"Selected dlc.rpf: {SelectedDlcRpfPath}");
        }
        catch (Exception ex)
        {
            AddLog($"Select dlc.rpf failed: {ex.Message}");
        }
    }

    private async Task InstallModAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var request = new ModInstallRequest
            {
                GtaPath = GtaPath.Trim(),
                SourceDlcRpfPath = SelectedDlcRpfPath.Trim(),
                ModName = ModName.Trim(),
                SpawnName = SpawnName.Trim(),
                AutoEditDlclist = true
            };

            await Task.Run(async () => await _modInstallService.InstallModAsync(request, AddLog));
            await LoadInstalledModsAsync();
            await SaveAppSettingsAsync();
            MessageBox.Show("Mod installation completed.", "Install Mod", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            AddLog($"Install failed: {ex.Message}");
            MessageBox.Show(ex.Message, "Install Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UninstallSelectedModAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (SelectedInstalledMod is null)
        {
            const string message = "Please select a mod to uninstall.";
            AddLog(message);
            MessageBox.Show(message, "Uninstall Mod", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"Uninstall mod '{SelectedInstalledMod.Name}'? This will delete the mod folder inside mods\\update\\x64\\dlcpacks.",
            "Confirm Uninstall",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var mod = SelectedInstalledMod;
            await Task.Run(async () => await _modInstallService.UninstallModAsync(GtaPath.Trim(), mod, AutoEditDlclist, AddLog));
            await LoadInstalledModsAsync();
            MessageBox.Show("Mod uninstall completed.", "Uninstall Mod", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            AddLog($"Uninstall failed: {ex.Message}");
            MessageBox.Show(ex.Message, "Uninstall Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void LaunchGtaOffline()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            _modInstallService.LaunchGtaOffline(GtaPath.Trim(), AddLog);
        }
        catch (Exception ex)
        {
            AddLog($"Launch failed: {ex.Message}");
            MessageBox.Show(ex.Message, "Launch Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadInstalledModsAsync()
    {
        try
        {
            var installedModsFile = await _jsonStorageService.LoadInstalledModsAsync();
            var orderedMods = installedModsFile.Mods
                .OrderByDescending(x => x.InstalledAt)
                .ToList();

            await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
            {
                InstalledMods.Clear();
                foreach (var mod in orderedMods)
                {
                    InstalledMods.Add(mod);
                }
            });

            AddLog($"Loaded {orderedMods.Count} installed mod record(s).");
        }
        catch (Exception ex)
        {
            AddLog($"Load installed mods failed: {ex.Message}");
        }
    }

    private async Task SaveAppSettingsAsync()
    {
        try
        {
            var settings = new AppSettings
            {
                GtaPath = GtaPath.Trim(),
                AutoEditDlclist = true
            };

            await _jsonStorageService.SaveAppSettingsAsync(settings);
        }
        catch (Exception ex)
        {
            AddLog($"Save settings failed: {ex.Message}");
        }
    }

    private void CopyLogEntries()
    {
        if (LogEntries.Count == 0)
        {
            return;
        }

        try
        {
            var fullLog = string.Join(Environment.NewLine, LogEntries);
            WpfClipboard.SetText(fullLog);
            AddLog($"Copied {LogEntries.Count} log entr{(LogEntries.Count == 1 ? "y" : "ies")} to clipboard.");
        }
        catch (Exception ex)
        {
            AddLog($"Copy log failed: {ex.Message}");
            MessageBox.Show(ex.Message, "Copy Log Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddLog(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        if (WpfApplication.Current.Dispatcher.CheckAccess())
        {
            LogEntries.Add(line);
            return;
        }

        WpfApplication.Current.Dispatcher.Invoke(() => LogEntries.Add(line));
    }

    private void RaiseCommandStates()
    {
        (BrowseGtaFolderCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (SelectDlcRpfCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (InstallModCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (UninstallSelectedModCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (LaunchGtaOfflineCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (RefreshInstalledModsCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (CopyLogEntriesCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }
}
