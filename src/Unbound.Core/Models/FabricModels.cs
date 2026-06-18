using System.Text.Json.Serialization;

namespace Unbound.Core.Models;

/// <summary>One entry from <c>/v2/versions/loader/{game}</c> on the Fabric Meta API.</summary>
public sealed class FabricLoaderEntry
{
    [JsonPropertyName("loader")] public FabricLoaderInfo Loader { get; set; } = new();
}

public sealed class FabricLoaderInfo
{
    [JsonPropertyName("version")] public string Version { get; set; } = "";
    [JsonPropertyName("stable")] public bool Stable { get; set; }
}

/// <summary>
/// The Mojang-style launcher version JSON returned by
/// <c>/v2/versions/loader/{game}/{loader}/profile/json</c>. We only model the fields we need;
/// the raw text is written to disk verbatim.
/// </summary>
public sealed class FabricProfileJson
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("libraries")] public List<FabricLibrary> Libraries { get; set; } = new();
}

public sealed class FabricLibrary
{
    /// <summary>Maven coordinate, e.g. <c>net.fabricmc:fabric-loader:0.16.10</c>.</summary>
    [JsonPropertyName("name")] public string Name { get; set; } = "";

    /// <summary>Base Maven repository URL the artifact is fetched from.</summary>
    [JsonPropertyName("url")] public string? Url { get; set; }
}
