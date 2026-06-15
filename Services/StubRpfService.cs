namespace GtavOfflineModLauncher.Services;

public sealed class StubRpfService : IRpfService
{
    private const string Message = "RPF editing chưa được tích hợp. Hãy tích hợp CodeWalker.Core hoặc rpf helper để sửa dlclist.xml bên trong update.rpf.";

    public string ExtractTextFile(string rpfPath, string internalPath)
    {
        throw new NotImplementedException(Message);
    }

    public void ReplaceTextFile(string rpfPath, string internalPath, string content)
    {
        throw new NotImplementedException(Message);
    }
}
