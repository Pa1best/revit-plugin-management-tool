using RevitPluginsManager.Services;
using Xunit;

namespace RevitPluginsManager.Tests;

public sealed class AddinServicesTests
{
    [Fact]
    public void Toggle_disable_then_enable_roundtrip()
    {
        var dir = Path.Combine(Path.GetTempPath(), "RevitPluginsManagerTests_" + Guid.NewGuid().ToString("N"));
        var yearDir = Path.Combine(dir, "2025");
        Directory.CreateDirectory(yearDir);
        try
        {
            var path = Path.Combine(yearDir, "Test.addin");
            const string manifestXml = "<RevitAddIns />";
            File.WriteAllText(path, manifestXml);

            var svc = new AddinToggleService(dir);
            var disabled = svc.SetEnabled(path, false);
            var expectedDisabled = Path.Combine(dir, RevitAddinScanner.DisabledFolderName, "2025", "Test.addin");
            Assert.Equal(expectedDisabled, disabled);
            Assert.False(File.Exists(path));
            Assert.True(File.Exists(disabled));

            var enabled = svc.SetEnabled(disabled, true);
            Assert.Equal(path, enabled);
            Assert.True(File.Exists(enabled));
            Assert.False(File.Exists(disabled));
            Assert.Equal(manifestXml, File.ReadAllText(enabled));
        }
        finally
        {
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch
            {
                // ignored
            }
        }
    }

    [Fact]
    public void Scanner_finds_year_folder_and_lists_addins()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "RevitScanTest_" + Guid.NewGuid().ToString("N"));
        var yearDir = Path.Combine(tempRoot, "2099");
        var disabledYearDir = Path.Combine(tempRoot, RevitAddinScanner.DisabledFolderName, "2099");
        Directory.CreateDirectory(yearDir);
        Directory.CreateDirectory(disabledYearDir);
        try
        {
            File.WriteAllText(Path.Combine(yearDir, "UnitTest.addin"), "<RevitAddIns />");
            File.WriteAllText(Path.Combine(disabledYearDir, "UnitOff.addin"), "<RevitAddIns />");

            var scanner = new RevitAddinScanner(tempRoot);
            Assert.Contains("2099", scanner.GetRevitVersions());

            var items = scanner.GetAddinsForVersion("2099");
            var names = items.Select(i => i.DisplayFileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            Assert.Contains("UnitTest.addin", names);
            Assert.Contains("UnitOff.addin", names);
            Assert.Single(items, i => i.DisplayFileName == "UnitTest.addin" && i.IsEnabled);
            Assert.Single(items, i => i.DisplayFileName == "UnitOff.addin" && !i.IsEnabled);
        }
        finally
        {
            try
            {
                Directory.Delete(tempRoot, recursive: true);
            }
            catch
            {
                // ignored
            }
        }
    }

    [Fact]
    public void Scanner_lists_disabled_only_year_without_active_folder()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "RevitScanTest_" + Guid.NewGuid().ToString("N"));
        var disabledYearDir = Path.Combine(tempRoot, RevitAddinScanner.DisabledFolderName, "2098");
        Directory.CreateDirectory(disabledYearDir);
        try
        {
            File.WriteAllText(Path.Combine(disabledYearDir, "OnlyOff.addin"), "<RevitAddIns />");

            var scanner = new RevitAddinScanner(tempRoot);
            Assert.Contains("2098", scanner.GetRevitVersions());

            var items = scanner.GetAddinsForVersion("2098");
            Assert.Single(items);
            Assert.Equal("OnlyOff.addin", items[0].DisplayFileName);
            Assert.False(items[0].IsEnabled);
        }
        finally
        {
            try
            {
                Directory.Delete(tempRoot, recursive: true);
            }
            catch
            {
                // ignored
            }
        }
    }
}
