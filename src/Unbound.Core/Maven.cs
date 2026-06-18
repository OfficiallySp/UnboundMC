namespace Unbound.Core;

/// <summary>Resolves Maven coordinates (as used in Fabric's library list) to repository paths/URLs.</summary>
public static class Maven
{
    /// <summary>
    /// "group:artifact:version[:classifier]" ->
    /// "group/as/path/artifact/version/artifact-version[-classifier].jar".
    /// </summary>
    public static string CoordinateToPath(string coordinate)
    {
        var parts = coordinate.Split(':');
        if (parts.Length < 3)
            throw new FormatException($"Invalid Maven coordinate: '{coordinate}'");

        var group = parts[0].Replace('.', '/');
        var artifact = parts[1];
        var version = parts[2];
        var classifier = parts.Length > 3 && parts[3].Length > 0 ? "-" + parts[3] : "";
        return $"{group}/{artifact}/{version}/{artifact}-{version}{classifier}.jar";
    }

    public static string ToUrl(string baseUrl, string coordinate)
    {
        if (!baseUrl.EndsWith('/')) baseUrl += "/";
        return baseUrl + CoordinateToPath(coordinate);
    }
}
