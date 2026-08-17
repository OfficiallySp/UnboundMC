using System.Text.Json;

namespace Unbound.Core;

/// <summary>
/// Writes No Chat Restrictions' own config file (<c>config/NoChatRestrictions.json</c>), which since
/// the mod's telemetry/profanity update lets users opt back into Mojang telemetry and the chat
/// profanity filter. Both default to <c>false</c> (fully unrestricted); Unbound only writes this
/// file when the user opts into one of them.
/// </summary>
public static class NoChatRestrictionsConfig
{
    public const string RelativePath = "config/NoChatRestrictions.json";
    private const string BackupSuffix = ".unbound-bak";

    private static string PathFor(string mcPath) => Path.Combine(mcPath, "config", "NoChatRestrictions.json");

    /// <summary>
    /// Writes the config with the given flags, backing up any existing file to
    /// <c>NoChatRestrictions.json.unbound-bak</c> first. The JSON keys match the mod's fields
    /// (<c>allowTelemetry</c>, <c>allowProfanityFilter</c>) exactly so GSON reads them.
    /// </summary>
    public static void Write(string mcPath, bool allowTelemetry, bool allowProfanityFilter)
    {
        var file = PathFor(mcPath);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);

        // Preserve an existing config (mod- or user-written) before we overwrite it.
        if (File.Exists(file) && !File.Exists(file + BackupSuffix))
            File.Copy(file, file + BackupSuffix);

        // Anonymous-member names ARE the JSON keys — keep them camelCase to match the mod.
        var json = JsonSerializer.Serialize(
            new { allowTelemetry, allowProfanityFilter },
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(file, json);
    }

    /// <summary>
    /// Reverts what <see cref="Write"/> did: restores the backup if we made one, otherwise deletes
    /// the config Unbound wrote. Safe to call when nothing exists.
    /// </summary>
    public static void Restore(string mcPath)
    {
        var file = PathFor(mcPath);
        var backup = file + BackupSuffix;
        if (File.Exists(backup))
        {
            File.Copy(backup, file, overwrite: true);
            File.Delete(backup);
        }
        else if (File.Exists(file))
        {
            File.Delete(file);
        }
    }
}
