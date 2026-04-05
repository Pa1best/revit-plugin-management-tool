using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using RevitPluginsManager.ViewModels;

namespace RevitPluginsManager;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();
        var vm = new MainViewModel();
        DataContext = vm;
        Loaded += (_, _) => vm.LoadVersions();

        // Windows 11: Tabbed = DWM wallpaper-linked backdrop (Settings: «Эффекты фона»).
        ApplicationThemeManager.Apply(
            ApplicationTheme.Dark,
            WindowBackdropType.Tabbed,
            updateAccent: true);
    }
}
