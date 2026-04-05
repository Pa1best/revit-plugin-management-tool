using System.IO;
using System.Text.RegularExpressions;

namespace RevitPluginsManager.Services;

public sealed class AddinToggleService
{
    private static readonly Regex YearFolder = new(@"^\d{4}$", RegexOptions.Compiled);

    private readonly string _addinsRoot;

    /// <param name="addinsRootOverride">For tests; default is roaming Autodesk Revit Addins folder (no username in path — uses <see cref="Environment.SpecialFolder.ApplicationData"/>).</param>
    public AddinToggleService(string? addinsRootOverride = null)
    {
        _addinsRoot = Path.GetFullPath(addinsRootOverride ?? RevitAddinScanner.GetAddinsRootPath());
    }

    /// <summary>
    /// Moves the manifest between <c>Addins\{year}\</c> and <c>Addins\.disabled\{year}\</c> (filename unchanged).
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

        var disabledRoot = Path.GetFullPath(Path.Combine(_addinsRoot, RevitAddinScanner.DisabledFolderName));

        var underDisabled = TryParseUnderDisabledRoot(fullPath, disabledRoot, fileName, out var versionDisabled);
        var underEnabled = TryParseUnderAddinsYear(fullPath, fileName, out var versionEnabled);

        if (enable)
        {
            if (underEnabled)
                return fullPath;

            if (!underDisabled)
                throw new InvalidOperationException(
                    "Enable only applies to manifests under the .disabled folder for this Revit Addins root.");

            var destDir = Path.GetFullPath(Path.Combine(_addinsRoot, versionDisabled!));
            var dest = Path.Combine(destDir, fileName);
            if (File.Exists(dest))
                throw new IOException($"Cannot enable: a file already exists: {dest}");

            Directory.CreateDirectory(destDir);
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
        if (File.Exists(disabledPath))
            throw new IOException($"Cannot disable: a file already exists: {disabledPath}");

        Directory.CreateDirectory(disabledDir);
        File.Move(fullPath, disabledPath);
        return disabledPath;
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
    private bool TryParseUnderAddinsYear(
        string fullPath,
        string fileName,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? version)
    {
        version = null;
        var rel = Path.GetRelativePath(_addinsRoot, fullPath);
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
