using System.Net.Http.Json;
using System.Text.Json;
using Unbound.Core.Models;

namespace Unbound.Core;

/// <summary>The parsed-and-raw Fabric profile JSON ready to be written into <c>.minecraft/versions</c>.</summary>
public sealed record FabricProfile(string Id, string RawJson, IReadOnlyList<FabricLibrary> Libraries);

/// <summary>Talks to the Fabric Meta API (https://meta.fabricmc.net).</summary>
public sealed class FabricMetaClient(HttpClient http)
{
    private const string Base = "https://meta.fabricmc.net/v2";
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Newest stable loader for the given Minecraft version (falls back to newest overall).</summary>
    public async Task<string> GetBestLoaderVersionAsync(string mcVersion, CancellationToken ct = default)
    {
        var url = $"{Base}/versions/loader/{Uri.EscapeDataString(mcVersion)}";
        var entries = await http.GetFromJsonAsync<List<FabricLoaderEntry>>(url, Json, ct)
                      ?? throw new InvalidOperationException("Fabric Meta returned no loader data.");
        if (entries.Count == 0)
            throw new NotSupportedException($"Fabric has no loader for Minecraft {mcVersion}.");

        // Fabric Meta returns newest-first.
        var stable = entries.FirstOrDefault(e => e.Loader.Stable);
        return (stable ?? entries[0]).Loader.Version;
    }

    /// <summary>The launcher-ready version JSON for a (Minecraft, loader) pair, plus its library list.</summary>
    public async Task<FabricProfile> GetProfileAsync(string mcVersion, string loaderVersion, CancellationToken ct = default)
    {
        var url = $"{Base}/versions/loader/{Uri.EscapeDataString(mcVersion)}/{Uri.EscapeDataString(loaderVersion)}/profile/json";
        var raw = await http.GetStringAsync(url, ct);
        var parsed = JsonSerializer.Deserialize<FabricProfileJson>(raw, Json)
                     ?? throw new InvalidOperationException("Could not parse the Fabric profile JSON.");
        return new FabricProfile(parsed.Id, raw, parsed.Libraries);
    }
}
