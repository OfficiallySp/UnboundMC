using Unbound.Core;
using Unbound.Core.Models;

namespace Unbound.Core.Tests;

public class ModrinthSelectionTests
{
    private static ModrinthVersion V(string num, string type, string published, params string[] games) => new()
    {
        VersionNumber = num,
        VersionType = type,
        GameVersions = games.ToList(),
        Loaders = new() { "fabric" },
        DatePublished = DateTimeOffset.Parse(published),
        Files = new() { new ModrinthFile { Url = "u", Filename = num + ".jar", Primary = true } },
    };

    [Fact]
    public void SelectBest_picksNewestReleaseForVersionAndLoader()
    {
        var versions = new List<ModrinthVersion>
        {
            V("1.0", "release", "2025-01-01", "1.21.1"),
            V("1.2", "release", "2025-03-01", "1.21.1"),
            V("1.1", "release", "2025-02-01", "1.21.1"),
            V("9.9", "release", "2025-09-01", "1.20.1"), // wrong game version
        };

        var best = ModrinthClient.SelectBest(versions, "1.21.1", "fabric");

        Assert.NotNull(best);
        Assert.Equal("1.2", best!.VersionNumber);
    }

    [Fact]
    public void SelectBest_prefersReleaseOverNewerBeta()
    {
        var versions = new List<ModrinthVersion>
        {
            V("2.0-beta", "beta", "2025-05-01", "1.21.1"),
            V("1.9", "release", "2025-04-01", "1.21.1"),
        };

        var best = ModrinthClient.SelectBest(versions, "1.21.1", "fabric");

        Assert.Equal("1.9", best!.VersionNumber);
    }

    [Fact]
    public void SelectBest_returnsNullWhenNothingMatches()
        => Assert.Null(ModrinthClient.SelectBest(
            new List<ModrinthVersion> { V("1.0", "release", "2025-01-01", "1.19.2") },
            "1.21.1", "fabric"));

    [Fact]
    public void PrimaryFile_prefersPrimary()
    {
        var v = new ModrinthVersion
        {
            Files =
            {
                new ModrinthFile { Filename = "sources.jar", Primary = false },
                new ModrinthFile { Filename = "main.jar", Primary = true },
            },
        };
        Assert.Equal("main.jar", ModrinthClient.PrimaryFile(v).Filename);
    }
}
