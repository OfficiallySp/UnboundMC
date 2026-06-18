using System.Text.Json;
using Unbound.Core.Models;

namespace Unbound.Core;

/// <summary>Reverts a Unbound install: removes the launcher profile and the mod jars we added.</summary>
public static class UnboundUninstaller
{
    private const string ModsManifestFile = ".unbound-install.json";
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Returns true if anything was removed.</summary>
    public static bool Uninstall(string mcPath)
    {
        var modsDir = Path.Combine(mcPath, "mods");
        var manifestPath = Path.Combine(modsDir, ModsManifestFile);
        if (!File.Exists(manifestPath)) return false;

        bool removed = false;
        try
        {
            var manifest = JsonSerializer.Deserialize<InstallManifest>(File.ReadAllText(manifestPath), Json);
            if (manifest is not null)
            {
                foreach (var mod in manifest.Mods)
                {
                    var f = Path.Combine(modsDir, mod.FileName);
                    if (File.Exists(f)) { File.Delete(f); removed = true; }
                }
                if (!string.IsNullOrEmpty(manifest.ProfileKey))
                {
                    LauncherProfiles.Remove(mcPath, manifest.ProfileKey);
                    removed = true;
                }
            }
        }
        catch { /* malformed manifest: still remove it below */ }

        File.Delete(manifestPath);
        return removed || true;
    }
}
