using System.Xml;
using System.Xml.Linq;
using GtavOfflineModLauncher.Helpers;

namespace GtavOfflineModLauncher.Services;

public sealed class DlcListService
{
    public string AddDlcPackEntry(string xmlContent, string modName)
    {
        if (!ValidationHelper.TryValidateModName(modName, out var error))
        {
            throw new ArgumentException(error, nameof(modName));
        }

        var document = ParseDocument(xmlContent);
        var pathsElement = document.Descendants("Paths").FirstOrDefault()
            ?? throw new InvalidOperationException("dlclist.xml does not contain a closing </Paths> section.");

        var entryValue = BuildEntry(modName);
        var normalizedEntry = NormalizeEntry(entryValue);
        var exists = pathsElement.Elements("Item")
            .Any(item => string.Equals(NormalizeEntry(item.Value), normalizedEntry, StringComparison.OrdinalIgnoreCase));

        if (!exists)
        {
            // Add the DLC entry right before </Paths> while keeping the XML easy to read.
            pathsElement.Add(new XElement("Item", entryValue));
        }

        return ToFormattedXml(document);
    }

    public string RemoveDlcPackEntry(string xmlContent, string modName)
    {
        if (!ValidationHelper.TryValidateModName(modName, out var error))
        {
            throw new ArgumentException(error, nameof(modName));
        }

        var document = ParseDocument(xmlContent);
        var pathsElement = document.Descendants("Paths").FirstOrDefault()
            ?? throw new InvalidOperationException("dlclist.xml does not contain a closing </Paths> section.");

        var entryValue = BuildEntry(modName);
        var normalizedEntry = NormalizeEntry(entryValue);
        var itemsToRemove = pathsElement.Elements("Item")
            .Where(item => string.Equals(NormalizeEntry(item.Value), normalizedEntry, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var item in itemsToRemove)
        {
            item.Remove();
        }

        return ToFormattedXml(document);
    }

    private static string NormalizeEntry(string entry)
    {
        return entry.Trim()
            .Replace('\\', '/')
            .Trim('/');
    }

    public string BuildEntry(string modName) => $"dlcpacks:/{modName}/";

    private static XDocument ParseDocument(string xmlContent)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
        {
            throw new ArgumentException("XML content is empty.", nameof(xmlContent));
        }

        return XDocument.Parse(xmlContent, LoadOptions.PreserveWhitespace);
    }

    private static string ToFormattedXml(XDocument document)
    {
        var settings = new XmlWriterSettings
        {
            // Reformat the output so manual review in OpenIV stays readable.
            Indent = true,
            NewLineChars = Environment.NewLine,
            NewLineHandling = NewLineHandling.Replace,
            OmitXmlDeclaration = document.Declaration is null
        };

        using var stringWriter = new StringWriter();
        using var xmlWriter = XmlWriter.Create(stringWriter, settings);
        document.Save(xmlWriter);
        xmlWriter.Flush();
        return stringWriter.ToString();
    }
}
