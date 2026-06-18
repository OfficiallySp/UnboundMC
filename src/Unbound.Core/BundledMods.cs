using System.Reflection;
using System.Text.Json;

namespace Unbound.Core;

/// <summary>A fallback jar embedded in the installer, keyed by mod and Minecraft version.</summary>
public sealed record BundledJar(string ModKey, string MinecraftVersion, string FileName);

/// <summary>
/// Resolves offline fallback jars embedded under <c>assets/bundled/</c>. The embedded
/// <c>manifest.json</c> maps (mod, Minecraft version) to a jar file name.
/// </summary>
public static class BundledMods
{
    private const string ResourcePrefix = "Unbound.Core.assets.bundled.";
    private static readonly Assembly Asm = typeof(BundledMods).Assembly;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public static IReadOnlyList<BundledJar> Manifest { get; } = LoadManifest();

    private static IReadOnlyList<BundledJar> LoadManifest()
    {
        using var stream = Asm.GetManifestResourceStream(ResourcePrefix + "manifest.json");
        if (stream is null) return Array.Empty<BundledJar>();
        return JsonSerializer.Deserialize<List<BundledJar>>(stream, Json) ?? new List<BundledJar>();
    }

    /// <summary>Exact-match bundled jar for a mod at a given Minecraft version, or null.</summary>
    public static BundledJar? Find(string modKey, string mcVersion)
        => Manifest.FirstOrDefault(b => b.ModKey == modKey && b.MinecraftVersion == mcVersion);

    public static byte[]? Read(BundledJar jar)
    {
        using var stream = Asm.GetManifestResourceStream(ResourcePrefix + jar.FileName);
        if (stream is null) return null;
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
