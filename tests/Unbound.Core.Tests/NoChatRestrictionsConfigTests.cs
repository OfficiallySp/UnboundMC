using System.Text.Json;
using Unbound.Core;

namespace Unbound.Core.Tests;

public class NoChatRestrictionsConfigTests : IDisposable
{
    private readonly string _mc;
    private string ConfigFile => Path.Combine(_mc, "config", "NoChatRestrictions.json");
    private string BackupFile => ConfigFile + ".unbound-bak";

    public NoChatRestrictionsConfigTests()
    {
        _mc = Path.Combine(Path.GetTempPath(), "unbound-ncr-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_mc);
    }

    public void Dispose() => Directory.Delete(_mc, recursive: true);

    [Fact]
    public void Write_createsConfig_withExactMojangKeysAndValues()
    {
        NoChatRestrictionsConfig.Write(_mc, allowTelemetry: true, allowProfanityFilter: false);

        Assert.True(File.Exists(ConfigFile));
        using var doc = JsonDocument.Parse(File.ReadAllText(ConfigFile));
        var root = doc.RootElement;
        // Keys must match the mod's GSON field names exactly, or the mod ignores them.
        Assert.True(root.GetProperty("allowTelemetry").GetBoolean());
        Assert.False(root.GetProperty("allowProfanityFilter").GetBoolean());
    }

    [Fact]
    public void Write_backsUpAnExistingConfig()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigFile)!);
        File.WriteAllText(ConfigFile, "{ \"allowTelemetry\": false, \"allowProfanityFilter\": false }");

        NoChatRestrictionsConfig.Write(_mc, allowTelemetry: false, allowProfanityFilter: true);

        Assert.True(File.Exists(BackupFile), "existing config should be backed up before overwrite");
        using var doc = JsonDocument.Parse(File.ReadAllText(ConfigFile));
        Assert.True(doc.RootElement.GetProperty("allowProfanityFilter").GetBoolean());
    }

    [Fact]
    public void Restore_bringsBackTheBackedUpConfig()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigFile)!);
        const string original = "{ \"allowTelemetry\": true, \"allowProfanityFilter\": true }";
        File.WriteAllText(ConfigFile, original);
        NoChatRestrictionsConfig.Write(_mc, allowTelemetry: false, allowProfanityFilter: false);

        NoChatRestrictionsConfig.Restore(_mc);

        Assert.False(File.Exists(BackupFile), "backup should be consumed");
        Assert.Equal(original, File.ReadAllText(ConfigFile));
    }

    [Fact]
    public void Restore_deletesConfig_whenUnboundCreatedItFresh()
    {
        NoChatRestrictionsConfig.Write(_mc, allowTelemetry: true, allowProfanityFilter: false); // no prior config

        NoChatRestrictionsConfig.Restore(_mc);

        Assert.False(File.Exists(ConfigFile), "a config Unbound created should be removed on restore");
    }

    [Fact]
    public void Restore_isNoOp_whenNothingExists()
    {
        var ex = Record.Exception(() => NoChatRestrictionsConfig.Restore(_mc));
        Assert.Null(ex);
    }
}
