using RevitPluginsManager.Models;
using RevitPluginsManager.Services;
using Xunit;

namespace RevitPluginsManager.Tests;

public sealed class AddinServicesTests
{
    [Fact]
    public void Toggle_disable_then_enable_roundtrip_single_user()
    {
        var dir = NewTempDir();
        var muDir = NewTempDir();
        var yearDir = Path.Combine(dir, "2025");
        Directory.CreateDirectory(yearDir);
        try
        {
            var path = Path.Combine(yearDir, "Test.addin");
            const string manifestXml = "<RevitAddIns />";
            File.WriteAllText(path, manifestXml);

            var svc = new AddinToggleService(dir, muDir);
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
            TryDelete(dir);
            TryDelete(muDir);
        }
    }

    [Fact]
    public void Toggle_disable_then_enable_roundtrip_multi_user()
    {
        var suDir = NewTempDir();
        var muDir = NewTempDir();
        var yearDir = Path.Combine(muDir, "2025");
        Directory.CreateDirectory(yearDir);
        try
        {
            var path = Path.Combine(yearDir, "Shared.addin");
            File.WriteAllText(path, "<RevitAddIns />");

            var svc = new AddinToggleService(suDir, muDir);
            var disabled = svc.SetEnabled(path, false);
            var expectedDisabled = Path.Combine(muDir, RevitAddinScanner.DisabledFolderName, "2025", "Shared.addin");
            Assert.Equal(expectedDisabled, disabled);

            var enabled = svc.SetEnabled(disabled, true);
            Assert.Equal(path, enabled);
        }
        finally
        {
            TryDelete(suDir);
            TryDelete(muDir);
        }
    }

    [Fact]
    public void Toggle_rejects_path_outside_known_roots()
    {
        var suDir = NewTempDir();
        var muDir = NewTempDir();
        var stray = NewTempDir();
        var yearDir = Path.Combine(stray, "2025");
        Directory.CreateDirectory(yearDir);
        try
        {
            var path = Path.Combine(yearDir, "Stray.addin");
            File.WriteAllText(path, "<RevitAddIns />");

            var svc = new AddinToggleService(suDir, muDir);
            Assert.Throws<InvalidOperationException>(() => svc.SetEnabled(path, false));
        }
        finally
        {
            TryDelete(suDir);
            TryDelete(muDir);
            TryDelete(stray);
        }
    }

    [Fact]
    public void Scanner_finds_year_folder_and_lists_addins_single_user()
    {
        var suRoot = NewTempDir();
        var muRoot = NewTempDir();
        var yearDir = Path.Combine(suRoot, "2099");
        var disabledYearDir = Path.Combine(suRoot, RevitAddinScanner.DisabledFolderName, "2099");
        Directory.CreateDirectory(yearDir);
        Directory.CreateDirectory(disabledYearDir);
        try
        {
            File.WriteAllText(Path.Combine(yearDir, "UnitTest.addin"), "<RevitAddIns />");
            File.WriteAllText(Path.Combine(disabledYearDir, "UnitOff.addin"), "<RevitAddIns />");

            var scanner = new RevitAddinScanner(suRoot, muRoot);
            Assert.Contains("2099", scanner.GetRevitVersions());

            var items = scanner.GetAddinsForVersion("2099");
            Assert.Single(items, i => i.DisplayFileName == "UnitTest.addin" && i.IsEnabled && i.Scope == AddinScope.SingleUser);
            Assert.Single(items, i => i.DisplayFileName == "UnitOff.addin" && !i.IsEnabled && i.Scope == AddinScope.SingleUser);
            Assert.All(items, i => Assert.False(i.HasCollision));
        }
        finally
        {
            TryDelete(suRoot);
            TryDelete(muRoot);
        }
    }

    [Fact]
    public void Scanner_lists_disabled_only_year_without_active_folder()
    {
        var suRoot = NewTempDir();
        var muRoot = NewTempDir();
        var disabledYearDir = Path.Combine(suRoot, RevitAddinScanner.DisabledFolderName, "2098");
        Directory.CreateDirectory(disabledYearDir);
        try
        {
            File.WriteAllText(Path.Combine(disabledYearDir, "OnlyOff.addin"), "<RevitAddIns />");

            var scanner = new RevitAddinScanner(suRoot, muRoot);
            Assert.Contains("2098", scanner.GetRevitVersions());

            var items = scanner.GetAddinsForVersion("2098");
            Assert.Single(items);
            Assert.Equal("OnlyOff.addin", items[0].DisplayFileName);
            Assert.False(items[0].IsEnabled);
            Assert.Equal(AddinScope.SingleUser, items[0].Scope);
        }
        finally
        {
            TryDelete(suRoot);
            TryDelete(muRoot);
        }
    }

    [Fact]
    public void Scanner_lists_multi_user_addins_and_versions()
    {
        var suRoot = NewTempDir();
        var muRoot = NewTempDir();
        var muYearDir = Path.Combine(muRoot, "2025");
        Directory.CreateDirectory(muYearDir);
        try
        {
            File.WriteAllText(Path.Combine(muYearDir, "Shared.addin"), "<RevitAddIns />");

            var scanner = new RevitAddinScanner(suRoot, muRoot);
            Assert.Contains("2025", scanner.GetRevitVersions());

            var items = scanner.GetAddinsForVersion("2025");
            var item = Assert.Single(items);
            Assert.Equal("Shared.addin", item.DisplayFileName);
            Assert.True(item.IsEnabled);
            Assert.Equal(AddinScope.MultiUser, item.Scope);
            Assert.False(item.HasCollision);
        }
        finally
        {
            TryDelete(suRoot);
            TryDelete(muRoot);
        }
    }

    [Fact]
    public void Scanner_flags_collision_when_same_filename_in_both_scopes()
    {
        var suRoot = NewTempDir();
        var muRoot = NewTempDir();
        var suYearDir = Path.Combine(suRoot, "2025");
        var muYearDir = Path.Combine(muRoot, "2025");
        Directory.CreateDirectory(suYearDir);
        Directory.CreateDirectory(muYearDir);
        try
        {
            File.WriteAllText(Path.Combine(suYearDir, "Conflict.addin"), "<RevitAddIns />");
            File.WriteAllText(Path.Combine(muYearDir, "Conflict.addin"), "<RevitAddIns />");
            File.WriteAllText(Path.Combine(muYearDir, "OnlyMu.addin"), "<RevitAddIns />");

            var scanner = new RevitAddinScanner(suRoot, muRoot);
            var items = scanner.GetAddinsForVersion("2025");

            Assert.Equal(3, items.Count);

            var conflictSu = items.Single(i => i.Scope == AddinScope.SingleUser && i.DisplayFileName == "Conflict.addin");
            var conflictMu = items.Single(i => i.Scope == AddinScope.MultiUser && i.DisplayFileName == "Conflict.addin");
            var onlyMu = items.Single(i => i.DisplayFileName == "OnlyMu.addin");

            Assert.True(conflictSu.HasCollision);
            Assert.True(conflictMu.HasCollision);
            Assert.False(onlyMu.HasCollision);
        }
        finally
        {
            TryDelete(suRoot);
            TryDelete(muRoot);
        }
    }

    [Fact]
    public void Scanner_collision_also_detected_when_one_side_is_disabled()
    {
        var suRoot = NewTempDir();
        var muRoot = NewTempDir();
        var suDisabledDir = Path.Combine(suRoot, RevitAddinScanner.DisabledFolderName, "2025");
        var muYearDir = Path.Combine(muRoot, "2025");
        Directory.CreateDirectory(suDisabledDir);
        Directory.CreateDirectory(muYearDir);
        try
        {
            File.WriteAllText(Path.Combine(suDisabledDir, "Shadow.addin"), "<RevitAddIns />");
            File.WriteAllText(Path.Combine(muYearDir, "Shadow.addin"), "<RevitAddIns />");

            var scanner = new RevitAddinScanner(suRoot, muRoot);
            var items = scanner.GetAddinsForVersion("2025");

            Assert.All(items, i => Assert.True(i.HasCollision));
        }
        finally
        {
            TryDelete(suRoot);
            TryDelete(muRoot);
        }
    }

    [Fact]
    public void Scanner_dedupes_when_same_filename_in_active_and_disabled_within_same_scope()
    {
        var suRoot = NewTempDir();
        var muRoot = NewTempDir();
        var yearDir = Path.Combine(suRoot, "2025");
        var disabledYearDir = Path.Combine(suRoot, RevitAddinScanner.DisabledFolderName, "2025");
        Directory.CreateDirectory(yearDir);
        Directory.CreateDirectory(disabledYearDir);
        try
        {
            File.WriteAllText(Path.Combine(yearDir, "Dup.addin"), "<RevitAddIns />");
            File.WriteAllText(Path.Combine(disabledYearDir, "Dup.addin"), "<RevitAddIns />");

            var scanner = new RevitAddinScanner(suRoot, muRoot);
            var items = scanner.GetAddinsForVersion("2025");

            var item = Assert.Single(items, i => i.DisplayFileName == "Dup.addin");
            Assert.True(item.IsEnabled);
            Assert.Equal(Path.Combine(yearDir, "Dup.addin"), item.FullPath);
        }
        finally
        {
            TryDelete(suRoot);
            TryDelete(muRoot);
        }
    }

    [Fact]
    public void Toggle_disable_overwrites_stale_disabled_copy()
    {
        var suRoot = NewTempDir();
        var muRoot = NewTempDir();
        var yearDir = Path.Combine(suRoot, "2025");
        var disabledYearDir = Path.Combine(suRoot, RevitAddinScanner.DisabledFolderName, "2025");
        Directory.CreateDirectory(yearDir);
        Directory.CreateDirectory(disabledYearDir);
        try
        {
            var activePath = Path.Combine(yearDir, "Stale.addin");
            var stalePath = Path.Combine(disabledYearDir, "Stale.addin");
            File.WriteAllText(activePath, "<RevitAddIns>active</RevitAddIns>");
            File.WriteAllText(stalePath, "<RevitAddIns>stale</RevitAddIns>");

            var svc = new AddinToggleService(suRoot, muRoot);
            var disabled = svc.SetEnabled(activePath, false);

            Assert.Equal(stalePath, disabled);
            Assert.False(File.Exists(activePath));
            Assert.True(File.Exists(stalePath));
            Assert.Equal("<RevitAddIns>active</RevitAddIns>", File.ReadAllText(stalePath));
        }
        finally
        {
            TryDelete(suRoot);
            TryDelete(muRoot);
        }
    }

    [Fact]
    public void Toggle_enable_drops_orphan_when_active_copy_already_exists()
    {
        var suRoot = NewTempDir();
        var muRoot = NewTempDir();
        var yearDir = Path.Combine(suRoot, "2025");
        var disabledYearDir = Path.Combine(suRoot, RevitAddinScanner.DisabledFolderName, "2025");
        Directory.CreateDirectory(yearDir);
        Directory.CreateDirectory(disabledYearDir);
        try
        {
            var activePath = Path.Combine(yearDir, "Orphan.addin");
            var orphanPath = Path.Combine(disabledYearDir, "Orphan.addin");
            File.WriteAllText(activePath, "<RevitAddIns>live</RevitAddIns>");
            File.WriteAllText(orphanPath, "<RevitAddIns>orphan</RevitAddIns>");

            var svc = new AddinToggleService(suRoot, muRoot);
            var enabled = svc.SetEnabled(orphanPath, true);

            Assert.Equal(activePath, enabled);
            Assert.True(File.Exists(activePath));
            Assert.False(File.Exists(orphanPath));
            Assert.Equal("<RevitAddIns>live</RevitAddIns>", File.ReadAllText(activePath));
        }
        finally
        {
            TryDelete(suRoot);
            TryDelete(muRoot);
        }
    }

    private static string NewTempDir()
    {
        return Path.Combine(Path.GetTempPath(), "RevitPluginsManagerTests_" + Guid.NewGuid().ToString("N"));
    }

    private static void TryDelete(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
        catch
        {
            // ignored
        }
    }
}
