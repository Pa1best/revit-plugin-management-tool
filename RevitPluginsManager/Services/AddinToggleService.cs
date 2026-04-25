using System.IO;
using System.Text.RegularExpressions;

namespace RevitPluginsManager.Services;

public sealed class AddinToggleService
{
    private static readonly Regex YearFolder = new(@"^\d{4}$", RegexOptions.Compiled);

    private readonly string _singleUserRoot;
    private readonly string _multiUserRoot;

    /// <param name="singleUserRootOverride">For tests; default is roaming Autodesk Revit Addins folder.</param>
    /// <param name="multiUserRootOverride">For tests; default is the ProgramData Autodesk Revit Addins folder.</param>
    public AddinToggleService(string? singleUserRootOverride = null, string? multiUserRootOverride = null)
    {
        _singleUserRoot = Path.GetFullPath(singleUserRootOverride ?? RevitAddinScanner.GetSingleUserAddinsRootPath());
        _multiUserRoot = Path.GetFullPath(multiUserRootOverride ?? RevitAddinScanner.GetMultiUserAddinsRootPath());
    }

    /// <summary>
    /// Moves the manifest between <c>{root}\{year}\</c> and <c>{root}\.disabled\{year}\</c> (filename unchanged).
    /// The root (Single-User vs Multi-User) is inferred from <paramref name="currentPath"/>.
    /// </summary>
    /// <returns>New full path of the manifest file.</returns>
    public string SetEnabled(string currentPath, bool enable)
    {
        if (string.IsNullOrWhiteSpace(currentPath))
            throw new ArgumentException("Path is required.", nameof(currentPath));

        if (!File.Exists(currentPath))
            throw new FileNotFoundException("Add-in file was not found.", currentPath);

        var fullPath = Path.GetFullPath(currentPath);
        var fileName = Path.GetFileName(fullPath);
        if (!fileName.EndsWith(".addin", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Not a recognized add-in manifest file.");

        var addinsRoot = ResolveRootForPath(fullPath)
            ?? throw new InvalidOperationException(
                "Manifest is not located under a known Single-User or Multi-User Revit Addins root.");

        var disabledRoot = Path.GetFullPath(Path.Combine(addinsRoot, RevitAddinScanner.DisabledFolderName));

        var underDisabled = TryParseUnderDisabledRoot(fullPath, disabledRoot, fileName, out var versionDisabled);
        var underEnabled = TryParseUnderAddinsYear(fullPath, addinsRoot, fileName, out var versionEnabled);

        if (enable)
        {
            if (underEnabled)
                return fullPath;

            if (!underDisabled)
                throw new InvalidOperationException(
                    "Enable only applies to manifests under the .disabled folder for this Revit Addins root.");

            var destDir = Path.GetFullPath(Path.Combine(addinsRoot, versionDisabled!));
            var dest = Path.Combine(destDir, fileName);

            Directory.CreateDirectory(destDir);
            // If a stale enabled copy already lives at the destination, the file we're "enabling" is
            // really just an orphaned disabled copy that the scanner already deduped against the live one.
            // Keep the active copy intact and discard the stale disabled file.
            if (File.Exists(dest))
            {
                File.Delete(fullPath);
                return dest;
            }

            File.Move(fullPath, dest);
            return dest;
        }

        if (underDisabled)
            return fullPath;

        if (!underEnabled)
            throw new InvalidOperationException(
                "Disable only applies to manifests directly under a year folder (e.g. Addins\\2025\\).");

        var disabledDir = Path.GetFullPath(Path.Combine(disabledRoot, versionEnabled!));
        var disabledPath = Path.Combine(disabledDir, fileName);

        Directory.CreateDirectory(disabledDir);
        // A stale .disabled copy can linger if the user disabled, then re-installed and disabled again.
        // The live (enabled) file is the source of truth — overwrite the stale one.
        if (File.Exists(disabledPath))
            File.Delete(disabledPath);

        File.Move(fullPath, disabledPath);
        return disabledPath;
    }

    private string? ResolveRootForPath(string fullPath)
    {
        if (IsUnderRoot(fullPath, _singleUserRoot))
            return _singleUserRoot;
        if (IsUnderRoot(fullPath, _multiUserRoot))
            return _multiUserRoot;
        return null;
    }

    private static bool IsUnderRoot(string fullPath, string root)
    {
        var rel = Path.GetRelativePath(root, fullPath);
        return !rel.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(rel);
    }

    /// <summary>True if <paramref name="fullPath"/> is <c>disabledRoot\YYYY\fileName</c>.</summary>
    private static bool TryParseUnderDisabledRoot(
        string fullPath,
        string disabledRoot,
        string fileName,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? version)
    {
        version = null;
        var rel = Path.GetRelativePath(disabledRoot, fullPath);
        if (rel.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(rel))
            return false;

        var parts = SplitPathParts(rel);
        if (parts.Count != 2 || !string.Equals(parts[1], fileName, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!YearFolder.IsMatch(parts[0]))
            return false;

        version = parts[0];
        return true;
    }

    /// <summary>True if <paramref name="fullPath"/> is <c>addinsRoot\YYYY\fileName</c> (not under .disabled).</summary>
    private static bool TryParseUnderAddinsYear(
        string fullPath,
        string addinsRoot,
        string fileName,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? version)
    {
        version = null;
        var rel = Path.GetRelativePath(addinsRoot, fullPath);
        if (rel.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(rel))
            return false;

        var parts = SplitPathParts(rel);
        if (parts.Count != 2 || !string.Equals(parts[1], fileName, StringComparison.OrdinalIgnoreCase))
            return false;
        if (string.Equals(parts[0], RevitAddinScanner.DisabledFolderName, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!YearFolder.IsMatch(parts[0]))
            return false;

        version = parts[0];
        return true;
    }

    private static List<string> SplitPathParts(string relativePath)
    {
        return relativePath
            .Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }
}
