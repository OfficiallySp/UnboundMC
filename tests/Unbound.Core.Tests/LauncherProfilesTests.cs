using System.Text.Json.Nodes;
using Unbound.Core;

namespace Unbound.Core.Tests;

public class LauncherProfilesTests : IDisposable
{
    private readonly string _dir;

    public LauncherProfilesTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "unbound-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void AddOrUpdate_preservesExistingProfiles_andBacksUp()
    {
        var file = LauncherProfiles.PathFor(_dir);
        File.WriteAllText(file,
            """
            { "version": 3, "profiles": { "vanilla": { "name": "Latest Release", "lastVersionId": "1.21.1" } } }
            """);

        LauncherProfiles.AddOrUpdate(_dir, "unbound-1.21.1", "Unbound", "fabric-loader-0.16.10-1.21.1");

        Assert.True(File.Exists(file + ".unbound-bak"), "original should be backed up");

        var root = JsonNode.Parse(File.ReadAllText(file))!.AsObject();
        var profiles = root["profiles"]!.AsObject();
        Assert.True(profiles.ContainsKey("vanilla"), "existing profile must be preserved");
        Assert.Equal("Unbound", profiles["unbound-1.21.1"]!["name"]!.GetValue<string>());
        Assert.Equal("fabric-loader-0.16.10-1.21.1", profiles["unbound-1.21.1"]!["lastVersionId"]!.GetValue<string>());
    }

    [Fact]
    public void AddOrUpdate_createsFileWhenMissing()
    {
        LauncherProfiles.AddOrUpdate(_dir, "unbound-1.20.1", "Unbound", "fabric-loader-x-1.20.1");

        var root = JsonNode.Parse(File.ReadAllText(LauncherProfiles.PathFor(_dir)))!.AsObject();
        Assert.Equal("fabric-loader-x-1.20.1",
            root["profiles"]!["unbound-1.20.1"]!["lastVersionId"]!.GetValue<string>());
    }

    [Fact]
    public void Remove_deletesOnlyOurProfile()
    {
        File.WriteAllText(LauncherProfiles.PathFor(_dir),
            """
            { "version": 3, "profiles": { "vanilla": { "name": "v" }, "unbound-1.21.1": { "name": "Unbound" } } }
            """);

        LauncherProfiles.Remove(_dir, "unbound-1.21.1");

        var profiles = JsonNode.Parse(File.ReadAllText(LauncherProfiles.PathFor(_dir)))!
            .AsObject()["profiles"]!.AsObject();
        Assert.True(profiles.ContainsKey("vanilla"));
        Assert.False(profiles.ContainsKey("unbound-1.21.1"));
    }
}
