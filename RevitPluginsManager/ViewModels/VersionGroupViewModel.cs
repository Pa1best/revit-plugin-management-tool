using System.Collections.ObjectModel;

namespace RevitPluginsManager.ViewModels;

public sealed class VersionGroupViewModel
{
    public VersionGroupViewModel(string version)
    {
        Version = version;
    }

    public string Version { get; }

    public ObservableCollection<AddinItemViewModel> Addins { get; } = new();
}
