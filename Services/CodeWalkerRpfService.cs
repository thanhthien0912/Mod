using System.Text;
using CodeWalker.GameFiles;

namespace GtavOfflineModLauncher.Services;

public sealed class CodeWalkerRpfService : IRpfService
{
    public string ExtractTextFile(string rpfPath, string internalPath)
    {
        ValidateInputs(rpfPath, internalPath);
        EnsureKeysLoaded(rpfPath);

        var rpf = OpenArchive(rpfPath);
        var fileEntry = FindFileEntry(rpf, internalPath)
            ?? throw new FileNotFoundException($"Internal file '{internalPath}' was not found inside archive '{rpfPath}'.", internalPath);

        var bytes = rpf.ExtractFile(fileEntry);
        return DecodeText(bytes);
    }

    public void ReplaceTextFile(string rpfPath, string internalPath, string content)
    {
        ValidateInputs(rpfPath, internalPath);

        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        EnsureKeysLoaded(rpfPath);

        var rpf = OpenArchive(rpfPath);
        var parts = SplitInternalPath(internalPath);
        if (parts.Length < 2)
        {
            throw new InvalidOperationException("Internal path must include at least one directory and a file name.");
        }

        var fileName = parts[^1];
        var directory = EnsureDirectoryPath(rpf.Root, parts[..^1]);
        var bytes = EncodeText(content);

        // CreateFile overwrites an existing entry by deleting the old one and inserting the new content.
        RpfFile.CreateFile(directory, fileName, bytes, overwrite: true);

        // Rebuild/Defragment the archive to write the changes back to disk.
        RpfFile.Defragment(rpf, (msg, progress) => { }, false);
    }

    private static void EnsureKeysLoaded(string rpfPath)
    {
        if (GTA5Keys.PC_AES_KEY != null)
        {
            return;
        }

        var dir = Path.GetDirectoryName(rpfPath);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "GTA5.exe")))
            {
                GTA5Keys.LoadFromPath(dir);
                return;
            }
            dir = Path.GetDirectoryName(dir);
        }

        throw new InvalidOperationException("Could not locate GTA5.exe in any parent folders of the archive to load encryption keys.");
    }

    private static RpfFile OpenArchive(string rpfPath)
    {
        var relPath = Path.GetFileName(rpfPath);
        var rpf = new RpfFile(rpfPath, relPath);
        var errors = new List<string>();

        rpf.ScanStructure(_ => { }, error =>
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                errors.Add(error);
            }
        });

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
        }

        if (rpf.Root is null)
        {
            throw new InvalidOperationException($"Failed to open RPF archive '{rpfPath}'. Root directory was not loaded.");
        }

        return rpf;
    }

    private static RpfFileEntry? FindFileEntry(RpfFile rpf, string internalPath)
    {
        var parts = SplitInternalPath(internalPath);
        if (parts.Length < 2)
        {
            return null;
        }

        var current = rpf.Root;
        for (var i = 0; i < parts.Length - 1; i++)
        {
            current = current.Directories.FirstOrDefault(x => string.Equals(x.Name, parts[i], StringComparison.OrdinalIgnoreCase));
            if (current is null)
            {
                return null;
            }
        }

        return current.Files.FirstOrDefault(x => string.Equals(x.Name, parts[^1], StringComparison.OrdinalIgnoreCase));
    }

    private static RpfDirectoryEntry EnsureDirectoryPath(RpfDirectoryEntry root, IEnumerable<string> pathParts)
    {
        var current = root;
        foreach (var part in pathParts)
        {
            var existing = current.Directories.FirstOrDefault(x => string.Equals(x.Name, part, StringComparison.OrdinalIgnoreCase));
            current = existing ?? RpfFile.CreateDirectory(current, part);
        }

        return current;
    }

    private static string[] SplitInternalPath(string internalPath)
    {
        return internalPath
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string DecodeText(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private static byte[] EncodeText(string content)
    {
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(content);
    }

    private static void ValidateInputs(string rpfPath, string internalPath)
    {
        if (string.IsNullOrWhiteSpace(rpfPath))
        {
            throw new ArgumentException("RPF path is required.", nameof(rpfPath));
        }

        if (!File.Exists(rpfPath))
        {
            throw new FileNotFoundException("RPF archive was not found.", rpfPath);
        }

        if (string.IsNullOrWhiteSpace(internalPath))
        {
            throw new ArgumentException("Internal archive path is required.", nameof(internalPath));
        }
    }
}
