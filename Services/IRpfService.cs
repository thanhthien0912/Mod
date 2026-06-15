namespace GtavOfflineModLauncher.Services;

public interface IRpfService
{
    string ExtractTextFile(string rpfPath, string internalPath);
    void ReplaceTextFile(string rpfPath, string internalPath, string content);
}
