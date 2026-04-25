namespace RevitPluginsManager.Models;

/// <summary>
/// Snapshot of one manifest on disk under the year folder (enabled) or under <c>.disabled\{year}\</c> (disabled),
/// for either the Single-User (roaming AppData) or Multi-User (ProgramData) Revit Addins root.
/// </summary>
public sealed class AddinItem
{
    public required string Version { get; init; }

    /// <summary>Always shown as the enabled manifest name, e.g. MyPlugin.addin.</summary>
    public required string DisplayFileName { get; init; }

    public required string FullPath { get; init; }

    public bool IsEnabled { get; init; }

    public required AddinScope Scope { get; init; }

    /// <summary>True when a manifest with the same filename also exists in the other scope for the same Revit version.</summary>
    public bool HasCollision { get; init; }
}
