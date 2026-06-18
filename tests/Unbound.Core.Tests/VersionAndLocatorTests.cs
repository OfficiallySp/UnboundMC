using Unbound.Core;

namespace Unbound.Core.Tests;

public class VersionAndLocatorTests
{
    [Fact]
    public void VersionComparer_ordersNumericallyNotLexically()
    {
        var input = new[] { "1.21.9", "1.21.10", "1.20.1", "1.21.1" };
        var sorted = input.OrderByDescending(s => s, MinecraftVersionComparer.Instance).ToArray();
        Assert.Equal(new[] { "1.21.10", "1.21.9", "1.21.1", "1.20.1" }, sorted);
    }

    [Fact]
    public void VersionComparer_snapshotsSortBelowReleases()
    {
        Assert.True(MinecraftVersionComparer.Instance.Compare("1.21.1", "23w13a") > 0);
    }

    [Fact]
    public void DefaultPath_endsWithMinecraftFolder()
    {
        var path = MinecraftLocator.GetDefaultPath();
        if (OperatingSystem.IsWindows())
            Assert.EndsWith(".minecraft", path);
        else if (OperatingSystem.IsMacOS())
            Assert.EndsWith(Path.Combine("Application Support", "minecraft"), path);
        else
            Assert.EndsWith(".minecraft", path);
    }

    [Fact]
    public void LooksValid_falseForEmptyTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "unbound-empty-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try { Assert.False(MinecraftLocator.LooksValid(dir)); }
        finally { Directory.Delete(dir); }
    }
}
