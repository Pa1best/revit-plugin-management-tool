using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using RevitPluginsManager.Services;

namespace RevitPluginsManager.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly RevitAddinScanner _scanner = new();
    private readonly AddinToggleService _toggleService = new();

    public ObservableCollection<VersionGroupViewModel> VersionGroups { get; } = new();

    public string AddinsRootPath => RevitAddinScanner.GetAddinsRootPath();

    public bool HasVersionGroups => VersionGroups.Count > 0;

    public void LoadVersions()
    {
        VersionGroups.Clear();
        foreach (var v in _scanner.GetRevitVersions())
        {
            var group = new VersionGroupViewModel(v);
            foreach (var item in _scanner.GetAddinsForVersion(v))
                group.Addins.Add(new AddinItemViewModel(item, _toggleService));

            VersionGroups.Add(group);
        }

        OnPropertyChanged(nameof(HasVersionGroups));
    }
}
