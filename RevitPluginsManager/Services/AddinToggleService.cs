using System.IO;

namespace RevitPluginsManager.Services;

public sealed class AddinToggleService
{
    public const string DisabledSuffix = ".disabled";

    /// <summary>
    /// Enable or disable by renaming between *.addin and *.addin.disabled.
    /// </summary>
    /// <returns>New full path of the manifest file.</returns>
    public string SetEnabled(string currentPath, bool enable)
    {
        if (string.IsNullOrWhiteSpace(currentPath))
            throw new ArgumentException("Path is required.", nameof(currentPath));

        if (!File.Exists(currentPath))
            throw new FileNotFoundException("Add-in file was not found.", currentPath);

        var dir = Path.GetDirectoryName(currentPath);
        if (string.IsNullOrEmpty(dir))
            throw new InvalidOperationException("Invalid path.");

        var fileName = Path.GetFileName(currentPath);
        var isDisabledFile = fileName.EndsWith(".addin.disabled", StringComparison.OrdinalIgnoreCase);
        var isEnabledFile =
            fileName.EndsWith(".addin", StringComparison.OrdinalIgnoreCase) && !isDisabledFile;

        if (!isEnabledFile && !isDisabledFile)
            throw new InvalidOperationException("Not a recognized add-in manifest file.");

        if (enable)
        {
            if (isEnabledFile)
                return currentPath;

            var newName = fileName[..^DisabledSuffix.Length];
            if (!newName.EndsWith(".addin", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Unexpected disabled manifest name.");

            var dest = Path.Combine(dir, newName);
            if (File.Exists(dest))
                throw new IOException($"Cannot enable: a file already exists: {dest}");

            File.Move(currentPath, dest);
            return dest;
        }

        if (isDisabledFile)
            return currentPath;

        var disabledPath = currentPath + DisabledSuffix;
        if (File.Exists(disabledPath))
            throw new IOException($"Cannot disable: a file already exists: {disabledPath}");

        File.Move(currentPath, disabledPath);
        return disabledPath;
    }
}
