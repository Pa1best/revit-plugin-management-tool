using CommunityToolkit.Mvvm.ComponentModel;
using RevitPluginsManager.Models;
using RevitPluginsManager.Services;

namespace RevitPluginsManager.ViewModels;

public partial class AddinItemViewModel : ObservableObject
{
    private readonly AddinToggleService _toggleService;
    private bool _suppressIsEnabled;

    public AddinItemViewModel(AddinItem model, AddinToggleService toggleService)
    {
        _toggleService = toggleService;
        Version = model.Version;
        DisplayFileName = model.DisplayFileName;
        FullPath = model.FullPath;
        _isEnabled = model.IsEnabled;
    }

    public string Version { get; }

    public string DisplayFileName { get; }

    [ObservableProperty]
    private string _fullPath;

    [ObservableProperty]
    private bool _isEnabled;

    partial void OnIsEnabledChanged(bool value)
    {
        if (_suppressIsEnabled)
            return;

        try
        {
            var newPath = _toggleService.SetEnabled(FullPath, value);
            FullPath = newPath;
        }
        catch (Exception ex)
        {
            _suppressIsEnabled = true;
            IsEnabled = !value;
            _suppressIsEnabled = false;
            System.Windows.MessageBox.Show(
                $"Could not change add-in state.\n\n{ex.Message}",
                "Revit Add-ins Manager",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
    }
}
