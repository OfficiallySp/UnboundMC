using System.Text.Json;
using System.Text.Json.Nodes;

namespace Unbound.Core;

/// <summary>
/// Reads and edits the official launcher's <c>launcher_profiles.json</c> while preserving any
/// existing profiles and unknown fields. The original is backed up once before the first edit.
/// </summary>
public static class LauncherProfiles
{
    private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };

    public static string PathFor(string mcPath) => Path.Combine(mcPath, "launcher_profiles.json");

    public static void AddOrUpdate(string mcPath, string profileKey, string name, string lastVersionId)
    {
        var file = PathFor(mcPath);

        JsonObject root;
        if (File.Exists(file))
        {
            BackupOnce(file);
            root = JsonNode.Parse(File.ReadAllText(file)) as JsonObject ?? new JsonObject();
        }
        else
        {
            root = new JsonObject { ["version"] = 3 };
        }

        if (root["profiles"] is not JsonObject profiles)
        {
            profiles = new JsonObject();
            root["profiles"] = profiles;
        }

        var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var created = (profiles[profileKey] as JsonObject)?["created"]?.GetValue<string>() ?? now;

        profiles[profileKey] = new JsonObject
        {
            ["name"] = name,
            ["type"] = "custom",
            ["created"] = created,
            ["lastUsed"] = now,
            ["lastVersionId"] = lastVersionId,
            ["icon"] = "Crafting_Table",
        };

        File.WriteAllText(file, root.ToJsonString(Pretty));
    }

    public static void Remove(string mcPath, string profileKey)
    {
        var file = PathFor(mcPath);
        if (!File.Exists(file)) return;
        if (JsonNode.Parse(File.ReadAllText(file)) is not JsonObject root) return;
        if (root["profiles"] is JsonObject profiles && profiles.Remove(profileKey))
            File.WriteAllText(file, root.ToJsonString(Pretty));
    }

    private static void BackupOnce(string file)
    {
        var backup = file + ".unbound-bak";
        if (!File.Exists(backup)) File.Copy(file, backup);
    }
}
