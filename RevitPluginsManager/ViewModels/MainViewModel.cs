using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using RevitPluginsManager.Services;

namespace RevitPluginsManager.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly RevitAddinScanner _scanner;
    private readonly AddinToggleService _toggleService;

    public MainViewModel()
    {
        _scanner = new RevitAddinScanner();
        _toggleService = new AddinToggleService(_scanner.RootPath);
    }

    public ObservableCollection<VersionGroupViewModel> VersionGroups { get; } = new();

    public string AddinsRootPath => _scanner.RootPath;

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
