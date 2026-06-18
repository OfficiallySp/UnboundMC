using System.Text.Json.Serialization;

namespace Unbound.Core.Models;

/// <summary>A version entry from the Modrinth API (<c>/v2/project/{id}/version</c>).</summary>
public sealed class ModrinthVersion
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("version_number")] public string VersionNumber { get; set; } = "";
    [JsonPropertyName("version_type")] public string VersionType { get; set; } = "release"; // release|beta|alpha
    [JsonPropertyName("game_versions")] public List<string> GameVersions { get; set; } = new();
    [JsonPropertyName("loaders")] public List<string> Loaders { get; set; } = new();
    [JsonPropertyName("date_published")] public DateTimeOffset DatePublished { get; set; }
    [JsonPropertyName("files")] public List<ModrinthFile> Files { get; set; } = new();
}

public sealed class ModrinthFile
{
    [JsonPropertyName("url")] public string Url { get; set; } = "";
    [JsonPropertyName("filename")] public string Filename { get; set; } = "";
    [JsonPropertyName("primary")] public bool Primary { get; set; }
    [JsonPropertyName("hashes")] public ModrinthHashes Hashes { get; set; } = new();
}

public sealed class ModrinthHashes
{
    [JsonPropertyName("sha1")] public string? Sha1 { get; set; }
    [JsonPropertyName("sha512")] public string? Sha512 { get; set; }
}
