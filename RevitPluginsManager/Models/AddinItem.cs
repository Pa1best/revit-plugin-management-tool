namespace RevitPluginsManager.Models;

/// <summary>
/// Snapshot of one manifest on disk (either *.addin or *.addin.disabled).
/// </summary>
public sealed class AddinItem
{
    public required string Version { get; init; }

    /// <summary>Always shown as the enabled manifest name, e.g. MyPlugin.addin.</summary>
    public required string DisplayFileName { get; init; }

    public required string FullPath { get; init; }

    public bool IsEnabled { get; init; }
}
