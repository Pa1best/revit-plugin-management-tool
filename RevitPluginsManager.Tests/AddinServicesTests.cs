using RevitPluginsManager.Services;
using Xunit;

namespace RevitPluginsManager.Tests;

public sealed class AddinServicesTests
{
    [Fact]
    public void Toggle_disable_then_enable_roundtrip()
    {
        var dir = Path.Combine(Path.GetTempPath(), "RevitPluginsManagerTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var path = Path.Combine(dir, "Test.addin");
            File.WriteAllText(path, "<RevitAddIns />");

            var svc = new AddinToggleService();
            var disabled = svc.SetEnabled(path, false);
            Assert.EndsWith(".addin.disabled", disabled, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(path));
            Assert.True(File.Exists(disabled));

            var enabled = svc.SetEnabled(disabled, true);
            Assert.Equal(Path.Combine(dir, "Test.addin"), enabled);
            Assert.True(File.Exists(enabled));
            Assert.False(File.Exists(disabled));
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
        Directory.CreateDirectory(yearDir);
        try
        {
            File.WriteAllText(Path.Combine(yearDir, "UnitTest.addin"), "<RevitAddIns />");
            File.WriteAllText(Path.Combine(yearDir, "UnitOff.addin.disabled"), "<RevitAddIns />");

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
}
