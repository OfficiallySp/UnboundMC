namespace Unbound.Core;

/// <summary>
/// Moves leftover / third-party mod jars out of the way before an install so the Fabric loader
/// doesn't refuse to start with "Incompatible mod found". The target users often have stray jars
/// from earlier manual attempts; those crash the game even though Unbound's own set is correct.
/// Jars are <b>moved, not deleted</b> — into <c>mods/.unbound-disabled/</c> — so it's reversible.
/// </summary>
public static class ModsFolderGuard
{
    /// <summary>Subfolder (inside <c>mods/</c>) that sidelined jars are moved into. Not a valid
    /// Minecraft version, so the Fabric loader ignores it.</summary>
    public const string DisabledDirName = ".unbound-disabled";

    /// <summary>
    /// Moves every top-level <c>*.jar</c> in <paramref name="modsDir"/> into
    /// <c>mods/.unbound-disabled/</c>, returning the file names moved (empty if none).
    /// Call this <i>after</i> removing Unbound's own previously-installed jars and <i>before</i>
    /// writing the new set, so only foreign/leftover mods are swept.
    /// </summary>
    public static IReadOnlyList<string> SweepForeignJars(string modsDir)
    {
        if (!Directory.Exists(modsDir)) return Array.Empty<string>();

        var jars = Directory.GetFiles(modsDir, "*.jar", SearchOption.TopDirectoryOnly);
        if (jars.Length == 0) return Array.Empty<string>();

        var disabledDir = Path.Combine(modsDir, DisabledDirName);
        Directory.CreateDirectory(disabledDir);

        var moved = new List<string>();
        foreach (var jar in jars)
        {
            var name = Path.GetFileName(jar);
            var dest = Path.Combine(disabledDir, name);
            // A jar of the same name may have been sidelined by an earlier run — keep both.
            if (File.Exists(dest)) dest = Path.Combine(disabledDir, UniqueName(disabledDir, name));
            File.Move(jar, dest);
            moved.Add(name);
        }
        return moved;
    }

    /// <summary>Appends " (n)" before the extension until the name is free in <paramref name="dir"/>.</summary>
    private static string UniqueName(string dir, string fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        for (int n = 2; ; n++)
        {
            var candidate = $"{stem} ({n}){ext}";
            if (!File.Exists(Path.Combine(dir, candidate))) return candidate;
        }
    }
}
