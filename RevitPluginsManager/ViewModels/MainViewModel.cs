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
        _toggleService = new AddinToggleService(_scanner.SingleUserRootPath, _scanner.MultiUserRootPath);
    }

    public ObservableCollection<VersionGroupViewModel> VersionGroups { get; } = new();

    public string SingleUserRootPath => _scanner.SingleUserRootPath;

    public string MultiUserRootPath => _scanner.MultiUserRootPath;

    public bool HasVersionGroups => VersionGroups.Count > 0;

    [ObservableProperty]
    private bool _showAutodeskPlugins;

    partial void OnShowAutodeskPluginsChanged(bool value) => LoadVersions();

    public void LoadVersions()
    {
        VersionGroups.Clear();
        foreach (var v in _scanner.GetRevitVersions())
        {
            var items = _scanner.GetAddinsForVersion(v);
            if (!ShowAutodeskPlugins)
                items = items.Where(IsCustomAddin).ToList();

            if (items.Count == 0)
                continue;

            var group = new VersionGroupViewModel(v);
            foreach (var item in items)
                group.Addins.Add(new AddinItemViewModel(item, _toggleService));

            VersionGroups.Add(group);
        }

        OnPropertyChanged(nameof(HasVersionGroups));
    }

    private static bool IsCustomAddin(Models.AddinItem item) =>
        !item.DisplayFileName.StartsWith("Autodesk.", StringComparison.OrdinalIgnoreCase);
}
