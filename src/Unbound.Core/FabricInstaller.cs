using Unbound.Core.Models;

namespace Unbound.Core;

/// <summary>
/// Installs Fabric by writing files directly into <c>.minecraft</c> (no Fabric installer jar, no Java needed):
/// the version JSON + stub jar under <c>versions/</c>, and every loader library under <c>libraries/</c>.
/// </summary>
public sealed class FabricInstaller(HttpClient http)
{
    /// <summary>Returns the version id (the launcher's <c>lastVersionId</c>).</summary>
    public async Task<string> InstallAsync(
        string mcPath, FabricProfile profile, IProgress<InstallProgress>? progress = null, CancellationToken ct = default)
    {
        var versionDir = Path.Combine(mcPath, "versions", profile.Id);
        Directory.CreateDirectory(versionDir);
        await File.WriteAllTextAsync(Path.Combine(versionDir, profile.Id + ".json"), profile.RawJson, ct);

        // The launcher expects a jar next to the json; Fabric's is intentionally empty.
        var jarPath = Path.Combine(versionDir, profile.Id + ".jar");
        if (!File.Exists(jarPath))
            await File.WriteAllBytesAsync(jarPath, Array.Empty<byte>(), ct);

        var librariesRoot = Path.Combine(mcPath, "libraries");
        int total = profile.Libraries.Count;
        int i = 0;
        foreach (var lib in profile.Libraries)
        {
            ct.ThrowIfCancellationRequested();
            i++;
            if (string.IsNullOrWhiteSpace(lib.Name) || string.IsNullOrWhiteSpace(lib.Url))
                continue;

            var relative = Maven.CoordinateToPath(lib.Name).Replace('/', Path.DirectorySeparatorChar);
            var dest = Path.Combine(librariesRoot, relative);
            progress?.Report(new InstallProgress(
                InstallStage.DownloadingLibraries,
                $"Library {i}/{total}: {lib.Name}",
                total > 0 ? (double)i / total : null));

            if (File.Exists(dest) && new FileInfo(dest).Length > 0)
                continue; // already present from a vanilla/other install

            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            var bytes = await http.GetByteArrayAsync(Maven.ToUrl(lib.Url!, lib.Name), ct);
            await File.WriteAllBytesAsync(dest, bytes, ct);
        }

        return profile.Id;
    }
}
