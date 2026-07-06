using Unbound.Core;

namespace Unbound.Core.Tests;

public class ModsFolderGuardTests : IDisposable
{
    private readonly string _mods;

    public ModsFolderGuardTests()
    {
        _mods = Path.Combine(Path.GetTempPath(), "unbound-mods-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_mods);
    }

    public void Dispose() => Directory.Delete(_mods, recursive: true);

    private string Disabled(string name) => Path.Combine(_mods, ModsFolderGuard.DisabledDirName, name);
    private void WriteFile(string name, string content = "x") => File.WriteAllText(Path.Combine(_mods, name), content);

    [Fact]
    public void Sweep_movesForeignJars_intoDisabledFolder()
    {
        WriteFile("some-old-mod.jar");
        WriteFile("another.jar");

        var moved = ModsFolderGuard.SweepForeignJars(_mods);

        Assert.Equal(2, moved.Count);
        Assert.Contains("some-old-mod.jar", moved);
        Assert.False(File.Exists(Path.Combine(_mods, "some-old-mod.jar")), "jar should be moved out of mods/");
        Assert.True(File.Exists(Disabled("some-old-mod.jar")), "jar should land in .unbound-disabled");
        Assert.True(File.Exists(Disabled("another.jar")));
    }

    [Fact]
    public void Sweep_leavesNonJarFilesAlone()
    {
        WriteFile(".unbound-install.json");
        WriteFile("options.txt");
        WriteFile("old.jar");

        var moved = ModsFolderGuard.SweepForeignJars(_mods);

        Assert.Single(moved);
        Assert.True(File.Exists(Path.Combine(_mods, ".unbound-install.json")), "manifest must stay");
        Assert.True(File.Exists(Path.Combine(_mods, "options.txt")), "non-jar must stay");
    }

    [Fact]
    public void Sweep_withNoJars_returnsEmpty_andCreatesNoFolder()
    {
        WriteFile("readme.txt");

        var moved = ModsFolderGuard.SweepForeignJars(_mods);

        Assert.Empty(moved);
        Assert.False(Directory.Exists(Path.Combine(_mods, ModsFolderGuard.DisabledDirName)),
            "no disabled folder should be created when there's nothing to move");
    }

    [Fact]
    public void Sweep_doesNotReSweepAlreadyDisabledJars()
    {
        WriteFile("mod.jar");
        ModsFolderGuard.SweepForeignJars(_mods);      // moves mod.jar into .unbound-disabled

        var second = ModsFolderGuard.SweepForeignJars(_mods);   // nothing left at top level

        Assert.Empty(second);
        Assert.True(File.Exists(Disabled("mod.jar")));
    }

    [Fact]
    public void Sweep_keepsBothWhenNameCollidesWithAPreviouslyDisabledJar()
    {
        WriteFile("dup.jar", "first");
        ModsFolderGuard.SweepForeignJars(_mods);      // dup.jar -> .unbound-disabled/dup.jar
        WriteFile("dup.jar", "second");               // a new, different dup.jar appears

        var moved = ModsFolderGuard.SweepForeignJars(_mods);

        Assert.Single(moved);
        Assert.True(File.Exists(Disabled("dup.jar")), "original disabled jar preserved");
        Assert.True(File.Exists(Disabled("dup (2).jar")), "colliding jar kept under a unique name");
        Assert.Equal("first", File.ReadAllText(Disabled("dup.jar")));
        Assert.Equal("second", File.ReadAllText(Disabled("dup (2).jar")));
    }

    [Fact]
    public void Sweep_onMissingModsDir_returnsEmpty()
        => Assert.Empty(ModsFolderGuard.SweepForeignJars(Path.Combine(_mods, "does-not-exist")));
}
