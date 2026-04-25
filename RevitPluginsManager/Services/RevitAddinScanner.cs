using System.IO;
using System.Text.RegularExpressions;
using RevitPluginsManager.Models;

namespace RevitPluginsManager.Services;

public sealed class RevitAddinScanner
{
    private static readonly Regex YearFolder = new(@"^\d{4}$", RegexOptions.Compiled);

    /// <summary>Folder under each Revit Addins root where disabled manifests are stored, by year.</summary>
    public const string DisabledFolderName = ".disabled";

    private readonly string _singleUserRoot;
    private readonly string _multiUserRoot;

    /// <param name="singleUserRootOverride">For tests; default is roaming Autodesk Revit Addins folder.</param>
    /// <param name="multiUserRootOverride">For tests; default is the ProgramData Autodesk Revit Addins folder.</param>
    public RevitAddinScanner(string? singleUserRootOverride = null, string? multiUserRootOverride = null)
    {
        _singleUserRoot = singleUserRootOverride ?? GetSingleUserAddinsRootPath();
        _multiUserRoot = multiUserRootOverride ?? GetMultiUserAddinsRootPath();
    }

    public string SingleUserRootPath => _singleUserRoot;

    public string MultiUserRootPath => _multiUserRoot;

    public static string GetSingleUserAddinsRootPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Autodesk",
            "Revit",
            "Addins");
    }

    public static string GetMultiUserAddinsRootPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Autodesk",
            "Revit",
            "Addins");
    }

    /// <summary>Returns year folder names (e.g. 2025) found in either scope, sorted descending.</summary>
    public IReadOnlyList<string> GetRevitVersions()
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        CollectYearsFromRoot(_singleUserRoot, set);
        CollectYearsFromRoot(_multiUserRoot, set);
        return set.OrderDescending(StringComparer.Ordinal).ToList();
    }

    private static void CollectYearsFromRoot(string root, HashSet<string> set)
    {
        if (!Directory.Exists(root))
            return;

        foreach (var name in Directory.EnumerateDirectories(root).Select(Path.GetFileName))
        {
            if (name is not null && YearFolder.IsMatch(name))
                set.Add(name);
        }

        var disabledRoot = Path.Combine(root, DisabledFolderName);
        if (Directory.Exists(disabledRoot))
        {
            foreach (var name in Directory.EnumerateDirectories(disabledRoot).Select(Path.GetFileName))
            {
                if (name is not null && YearFolder.IsMatch(name))
                    set.Add(name);
            }
        }
    }

    public IReadOnlyList<AddinItem> GetAddinsForVersion(string version)
    {
        var singleUser = ScanScope(version, _singleUserRoot, AddinScope.SingleUser);
        var multiUser = ScanScope(version, _multiUserRoot, AddinScope.MultiUser);

        var singleUserNames = singleUser
            .Select(a => a.DisplayFileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var multiUserNames = multiUser
            .Select(a => a.DisplayFileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var combined = new List<AddinItem>(singleUser.Count + multiUser.Count);
        combined.AddRange(singleUser.Select(a => WithCollision(a, multiUserNames.Contains(a.DisplayFileName))));
        combined.AddRange(multiUser.Select(a => WithCollision(a, singleUserNames.Contains(a.DisplayFileName))));

        return combined
            .OrderBy(a => a.DisplayFileName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.Scope == AddinScope.SingleUser ? 0 : 1)
            .ToList();
    }

    private static AddinItem WithCollision(AddinItem source, bool hasCollision)
    {
        return new AddinItem
        {
            Version = source.Version,
            DisplayFileName = source.DisplayFileName,
            FullPath = source.FullPath,
            IsEnabled = source.IsEnabled,
            Scope = source.Scope,
            HasCollision = hasCollision,
        };
    }

    private static List<AddinItem> ScanScope(string version, string root, AddinScope scope)
    {
        // Dedupe by filename: an enabled copy in {root}\{year}\ wins over a stale copy in {root}\.disabled\{year}\
        // (Revit only loads the enabled one). The stale copy gets cleaned up the next time the user toggles the row.
        var byName = new Dictionary<string, AddinItem>(StringComparer.OrdinalIgnoreCase);

        var folder = Path.Combine(root, version);
        if (Directory.Exists(folder))
        {
            foreach (var path in Directory.EnumerateFiles(folder, "*.addin", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(path);
                byName[name] = new AddinItem
                {
                    Version = version,
                    DisplayFileName = name,
                    FullPath = path,
                    IsEnabled = true,
                    Scope = scope,
                };
            }
        }

        var disabledFolder = Path.Combine(root, DisabledFolderName, version);
        if (Directory.Exists(disabledFolder))
        {
            foreach (var path in Directory.EnumerateFiles(disabledFolder, "*.addin", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(path);
                if (byName.ContainsKey(name))
                    continue;

                byName[name] = new AddinItem
                {
                    Version = version,
                    DisplayFileName = name,
                    FullPath = path,
                    IsEnabled = false,
                    Scope = scope,
                };
            }
        }

        return byName.Values.ToList();
    }
}
