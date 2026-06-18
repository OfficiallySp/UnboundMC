using System.Net.Http.Json;
using System.Text.Json;
using Unbound.Core.Models;

namespace Unbound.Core;

/// <summary>Reads mod versions and downloads jars from the Modrinth API (https://api.modrinth.com).</summary>
public sealed class ModrinthClient(HttpClient http)
{
    private const string Base = "https://api.modrinth.com/v2";
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    // Official Modrinth project ids.
    public const string NoChatRestrictionsId = "z440MEwJ";
    public const string FabricApiId = "P7dR8mSH";

    public async Task<IReadOnlyList<ModrinthVersion>> GetVersionsAsync(
        string projectId, string mcVersion, string loader = "fabric", CancellationToken ct = default)
    {
        var url = $"{Base}/project/{projectId}/version?loaders=[\"{loader}\"]&game_versions=[\"{Uri.EscapeDataString(mcVersion)}\"]";
        return await http.GetFromJsonAsync<List<ModrinthVersion>>(url, Json, ct) ?? new List<ModrinthVersion>();
    }

    public async Task<ModrinthVersion?> GetBestVersionAsync(
        string projectId, string mcVersion, string loader = "fabric", CancellationToken ct = default)
        => SelectBest(await GetVersionsAsync(projectId, mcVersion, loader, ct), mcVersion, loader);

    /// <summary>Newest stable build matching the version + loader; prefers releases over beta/alpha.</summary>
    public static ModrinthVersion? SelectBest(IReadOnlyList<ModrinthVersion> versions, string mcVersion, string loader)
        => versions
            .Where(v => v.GameVersions.Contains(mcVersion) && v.Loaders.Contains(loader))
            .OrderByDescending(v => v.VersionType == "release")
            .ThenByDescending(v => v.DatePublished)
            .FirstOrDefault();

    public static ModrinthFile PrimaryFile(ModrinthVersion v)
        => v.Files.FirstOrDefault(f => f.Primary) ?? v.Files.First();

    /// <summary>All Minecraft versions this project publishes Fabric builds for, newest first.</summary>
    public async Task<IReadOnlyList<string>> GetSupportedGameVersionsAsync(
        string projectId, string loader = "fabric", CancellationToken ct = default)
    {
        var url = $"{Base}/project/{projectId}/version?loaders=[\"{loader}\"]";
        var list = await http.GetFromJsonAsync<List<ModrinthVersion>>(url, Json, ct) ?? new List<ModrinthVersion>();
        return list.SelectMany(v => v.GameVersions)
                   .Distinct()
                   .OrderByDescending(s => s, MinecraftVersionComparer.Instance)
                   .ToList();
    }

    /// <summary>Downloads the file and verifies its SHA-512 (throws on mismatch).</summary>
    public async Task<byte[]> DownloadAsync(ModrinthFile file, CancellationToken ct = default)
    {
        var bytes = await http.GetByteArrayAsync(file.Url, ct);
        if (!Hashing.VerifySha512(bytes, file.Hashes.Sha512))
            throw new InvalidOperationException($"SHA-512 mismatch for {file.Filename} — refusing to install.");
        return bytes;
    }
}
