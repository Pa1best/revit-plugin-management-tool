using System.IO;
using System.Text.RegularExpressions;
using RevitPluginsManager.Models;

namespace RevitPluginsManager.Services;

public sealed class RevitAddinScanner
{
    private static readonly Regex YearFolder = new(@"^\d{4}$", RegexOptions.Compiled);

    /// <summary>Folder under the Revit Addins root where disabled manifests are stored, by year.</summary>
    public const string DisabledFolderName = ".disabled";

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

    /// <summary>Returns year folder names (e.g. 2025), sorted descending — from active Addins and from <see cref="DisabledFolderName"/>.</summary>
    public IReadOnlyList<string> GetRevitVersions()
    {
        if (!Directory.Exists(_root))
            return [];

        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in Directory.EnumerateDirectories(_root).Select(Path.GetFileName))
        {
            if (name is not null && YearFolder.IsMatch(name))
                set.Add(name);
        }

        var disabledRoot = Path.Combine(_root, DisabledFolderName);
        if (Directory.Exists(disabledRoot))
        {
            foreach (var name in Directory.EnumerateDirectories(disabledRoot).Select(Path.GetFileName))
            {
                if (name is not null && YearFolder.IsMatch(name))
                    set.Add(name);
            }
        }

        return set.OrderDescending(StringComparer.Ordinal).ToList();
    }

    public IReadOnlyList<AddinItem> GetAddinsForVersion(string version)
    {
        var list = new List<AddinItem>();

        var folder = Path.Combine(_root, version);
        if (Directory.Exists(folder))
        {
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
        }

        var disabledFolder = Path.Combine(_root, DisabledFolderName, version);
        if (Directory.Exists(disabledFolder))
        {
            foreach (var path in Directory.EnumerateFiles(disabledFolder, "*.addin", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(path);
                list.Add(
                    new AddinItem
                    {
                        Version = version,
                        DisplayFileName = name,
                        FullPath = path,
                        IsEnabled = false,
                    });
            }
        }

        return list.OrderBy(a => a.DisplayFileName, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
