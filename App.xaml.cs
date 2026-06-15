using System.IO;
using System.Windows;

namespace GtavOfflineModLauncher;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        System.AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            try
            {
                File.WriteAllText("d:\\Mod\\GtavOfflineModLauncher\\crash_domain.txt", args.ExceptionObject?.ToString() ?? "Unknown exception");
            }
            catch { }
        };
        
        this.DispatcherUnhandledException += (sender, args) =>
        {
            try
            {
                File.WriteAllText("d:\\Mod\\GtavOfflineModLauncher\\crash_dispatcher.txt", args.Exception?.ToString() ?? "Unknown dispatcher exception");
            }
            catch { }
        };

        base.OnStartup(e);
    }
}
