using System.IO;
using System.Text.RegularExpressions;
using RevitPluginsManager.Models;

namespace RevitPluginsManager.Services;

public sealed class RevitAddinScanner
{
    private static readonly Regex YearFolder = new(@"^\d{4}$", RegexOptions.Compiled);

    private readonly string _root;

    /// <param name="addinsRootOverride">For tests; default is roaming Autodesk Revit Addins folder.</param>
    public RevitAddinScanner(string? addinsRootOverride = null)
    {
        _root = addinsRootOverride ?? GetAddinsRootPath();
    }

    public string RootPath => _root;

    public static string GetAddinsRootPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Autodesk",
            "Revit",
            "Addins");
    }

    /// <summary>Returns year folder names (e.g. 2025), sorted descending.</summary>
    public IReadOnlyList<string> GetRevitVersions()
    {
        if (!Directory.Exists(_root))
            return [];

        return Directory
            .EnumerateDirectories(_root)
            .Select(Path.GetFileName)
            .Where(name => name is not null && YearFolder.IsMatch(name))
            .Cast<string>()
            .OrderDescending(StringComparer.Ordinal)
            .ToList();
    }

    public IReadOnlyList<AddinItem> GetAddinsForVersion(string version)
    {
        var folder = Path.Combine(_root, version);
        if (!Directory.Exists(folder))
            return [];

        var list = new List<AddinItem>();

        foreach (var path in Directory.EnumerateFiles(folder, "*.addin", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileName(path);
            list.Add(
                new AddinItem
                {
                    Version = version,
                    DisplayFileName = name,
                    FullPath = path,
                    IsEnabled = true,
                });
        }

        foreach (var path in Directory.EnumerateFiles(folder, "*.addin.disabled", SearchOption.TopDirectoryOnly))
        {
            var fileName = Path.GetFileName(path);
            var display = fileName[..^".disabled".Length];
            list.Add(
                new AddinItem
                {
                    Version = version,
                    DisplayFileName = display,
                    FullPath = path,
                    IsEnabled = false,
                });
        }

        return list.OrderBy(a => a.DisplayFileName, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
