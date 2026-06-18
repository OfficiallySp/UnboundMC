namespace Unbound.Core.Models;

public enum InstallStage
{
    Locating,
    ResolvingFabric,
    InstallingFabric,
    DownloadingLibraries,
    InstallingMods,
    WritingProfile,
    Done,
}

/// <summary>Progress callback payload reported during installation.</summary>
public sealed record InstallProgress(InstallStage Stage, string Message, double? Fraction = null);

public sealed class InstallOptions
{
    public required string MinecraftPath { get; init; }
    public required string MinecraftVersion { get; init; }

    /// <summary>Skip all network access and install only from bundled fallback jars.</summary>
    public bool ForceOffline { get; init; }

    /// <summary>Label shown for the profile in the official launcher.</summary>
    public string ProfileName { get; init; } = "Unbound";
}

/// <summary>Where an installed jar came from.</summary>
public sealed record InstalledMod(string Name, string FileName, string Source); // Source: "modrinth" | "bundled"

public sealed class InstallResult
{
    public required string MinecraftVersion { get; init; }
    public required string LoaderVersion { get; init; }
    public required string ProfileId { get; init; }   // launcher lastVersionId
    public required string ProfileKey { get; init; }  // key under launcher_profiles.json:profiles
    public required string MinecraftPath { get; init; }
    public List<InstalledMod> Mods { get; } = new();
}

/// <summary>Persisted to <c>mods/.unbound-install.json</c> so re-installs are idempotent and uninstall is clean.</summary>
public sealed class InstallManifest
{
    public string Installer { get; set; } = "Unbound";
    public string MinecraftVersion { get; set; } = "";
    public string LoaderVersion { get; set; } = "";
    public string ProfileId { get; set; } = "";
    public string ProfileKey { get; set; } = "";
    public List<InstalledMod> Mods { get; set; } = new();
}
