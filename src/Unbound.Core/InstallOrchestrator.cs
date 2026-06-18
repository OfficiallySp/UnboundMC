using System.Text.Json;
using Unbound.Core.Models;

namespace Unbound.Core;

/// <summary>Drives the full install: Fabric (direct write) + Fabric API + No Chat Restrictions + launcher profile.</summary>
public sealed class InstallOrchestrator
{
    private const string ModsManifestFile = ".unbound-install.json";
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };

    private readonly HttpClient _http;
    private readonly FabricMetaClient _fabricMeta;
    private readonly FabricInstaller _fabricInstaller;
    private readonly ModrinthClient _modrinth;

    public InstallOrchestrator(HttpClient? http = null)
    {
        _http = http ?? UnboundHttp.Create();
        _fabricMeta = new FabricMetaClient(_http);
        _fabricInstaller = new FabricInstaller(_http);
        _modrinth = new ModrinthClient(_http);
    }

    public async Task<InstallResult> InstallAsync(
        InstallOptions opt, IProgress<InstallProgress>? progress = null, CancellationToken ct = default)
    {
        progress?.Report(new(InstallStage.Locating, $"Using {opt.MinecraftPath}"));
        if (!Directory.Exists(opt.MinecraftPath))
            throw new DirectoryNotFoundException($".minecraft not found at {opt.MinecraftPath}");

        // 1. Fabric loader (direct file write — no Java required).
        progress?.Report(new(InstallStage.ResolvingFabric, $"Finding Fabric for Minecraft {opt.MinecraftVersion}"));
        var loaderVersion = await _fabricMeta.GetBestLoaderVersionAsync(opt.MinecraftVersion, ct);
        progress?.Report(new(InstallStage.InstallingFabric, $"Installing Fabric loader {loaderVersion}"));
        var profile = await _fabricMeta.GetProfileAsync(opt.MinecraftVersion, loaderVersion, ct);
        var versionId = await _fabricInstaller.InstallAsync(opt.MinecraftPath, profile, progress, ct);

        // 2. Mods.
        var modsDir = Path.Combine(opt.MinecraftPath, "mods");
        Directory.CreateDirectory(modsDir);
        CleanPreviousInstall(modsDir);

        var result = new InstallResult
        {
            MinecraftVersion = opt.MinecraftVersion,
            LoaderVersion = loaderVersion,
            ProfileId = versionId,
            ProfileKey = "unbound-" + opt.MinecraftVersion,
            MinecraftPath = opt.MinecraftPath,
        };

        await InstallModAsync(modsDir, "fabric-api", ModrinthClient.FabricApiId, "Fabric API", opt, result, progress, ct);
        await InstallModAsync(modsDir, "no-chat-restrictions", ModrinthClient.NoChatRestrictionsId, "No Chat Restrictions", opt, result, progress, ct);

        WriteInstallManifest(modsDir, result);

        // 3. Launcher profile.
        progress?.Report(new(InstallStage.WritingProfile, $"Adding the \"{opt.ProfileName}\" launcher profile"));
        LauncherProfiles.AddOrUpdate(opt.MinecraftPath, result.ProfileKey, opt.ProfileName, versionId);

        progress?.Report(new(InstallStage.Done, "Installation complete"));
        return result;
    }

    private async Task InstallModAsync(
        string modsDir, string modKey, string projectId, string displayName,
        InstallOptions opt, InstallResult result, IProgress<InstallProgress>? progress, CancellationToken ct)
    {
        progress?.Report(new(InstallStage.InstallingMods, $"Installing {displayName}"));

        byte[]? bytes = null;
        string? fileName = null;
        string source = "bundled";

        // Prefer the live Modrinth build (always current), fall back to the bundled copy.
        if (!opt.ForceOffline)
        {
            try
            {
                var version = await _modrinth.GetBestVersionAsync(projectId, opt.MinecraftVersion, "fabric", ct);
                if (version is not null)
                {
                    var file = ModrinthClient.PrimaryFile(version);
                    bytes = await _modrinth.DownloadAsync(file, ct);
                    fileName = file.Filename;
                    source = "modrinth";
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                progress?.Report(new(InstallStage.InstallingMods,
                    $"{displayName}: download failed ({ex.Message}); trying bundled copy"));
            }
        }

        if (bytes is null)
        {
            var jar = BundledMods.Find(modKey, opt.MinecraftVersion);
            if (jar is not null)
            {
                bytes = BundledMods.Read(jar);
                fileName = jar.FileName;
                source = "bundled";
            }
        }

        if (bytes is null || fileName is null)
            throw new InvalidOperationException(
                $"Could not install {displayName} for Minecraft {opt.MinecraftVersion}: " +
                "no download succeeded and no bundled fallback is available for this version.");

        await File.WriteAllBytesAsync(Path.Combine(modsDir, fileName), bytes, ct);
        result.Mods.Add(new InstalledMod(displayName, fileName, source));
    }

    /// <summary>Deletes jars from a prior Unbound install so re-running doesn't leave conflicting duplicates.</summary>
    private static void CleanPreviousInstall(string modsDir)
    {
        var manifestPath = Path.Combine(modsDir, ModsManifestFile);
        if (!File.Exists(manifestPath)) return;
        try
        {
            var prev = JsonSerializer.Deserialize<InstallManifest>(File.ReadAllText(manifestPath), Json);
            if (prev is null) return;
            foreach (var mod in prev.Mods)
            {
                var f = Path.Combine(modsDir, mod.FileName);
                if (File.Exists(f)) File.Delete(f);
            }
        }
        catch { /* malformed manifest: ignore and overwrite */ }
    }

    private static void WriteInstallManifest(string modsDir, InstallResult result)
    {
        var manifest = new InstallManifest
        {
            MinecraftVersion = result.MinecraftVersion,
            LoaderVersion = result.LoaderVersion,
            ProfileId = result.ProfileId,
            ProfileKey = result.ProfileKey,
            Mods = result.Mods.ToList(),
        };
        File.WriteAllText(Path.Combine(modsDir, ModsManifestFile), JsonSerializer.Serialize(manifest, Json));
    }
}
