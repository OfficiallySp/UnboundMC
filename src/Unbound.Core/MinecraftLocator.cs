namespace Unbound.Core;

/// <summary>Finds the official-launcher <c>.minecraft</c> directory and inspects installed versions.</summary>
public static class MinecraftLocator
{
    public static string GetDefaultPath()
    {
        if (OperatingSystem.IsWindows())
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, ".minecraft");
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (OperatingSystem.IsMacOS())
            return Path.Combine(home, "Library", "Application Support", "minecraft");

        // Linux and other Unix
        return Path.Combine(home, ".minecraft");
    }

    /// <summary>A folder looks like a real install if it has the launcher profile file or a versions folder.</summary>
    public static bool LooksValid(string path) =>
        Directory.Exists(path) &&
        (File.Exists(Path.Combine(path, "launcher_profiles.json")) ||
         Directory.Exists(Path.Combine(path, "versions")));

    public static bool TryLocate(out string path)
    {
        path = GetDefaultPath();
        return LooksValid(path);
    }

    /// <summary>Version ids that have a real version JSON under <c>versions/</c> (vanilla + any modded profiles).</summary>
    public static IReadOnlyList<string> GetInstalledVersionIds(string mcPath)
    {
        var versionsDir = Path.Combine(mcPath, "versions");
        if (!Directory.Exists(versionsDir)) return Array.Empty<string>();

        var list = new List<string>();
        foreach (var dir in Directory.EnumerateDirectories(versionsDir))
        {
            var id = Path.GetFileName(dir);
            if (File.Exists(Path.Combine(dir, id + ".json")))
                list.Add(id);
        }
        return list;
    }
}
