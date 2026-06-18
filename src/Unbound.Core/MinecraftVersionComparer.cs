namespace Unbound.Core;

/// <summary>
/// Orders Minecraft version strings numerically by dotted components ("1.21.10" > "1.21.9").
/// Non-numeric components (snapshots like "23w13a") sort below numeric ones.
/// </summary>
public sealed class MinecraftVersionComparer : IComparer<string>
{
    public static readonly MinecraftVersionComparer Instance = new();

    public int Compare(string? x, string? y)
    {
        var xs = Parse(x);
        var ys = Parse(y);
        for (int i = 0; i < Math.Max(xs.Length, ys.Length); i++)
        {
            int a = i < xs.Length ? xs[i] : 0;
            int b = i < ys.Length ? ys[i] : 0;
            if (a != b) return a.CompareTo(b);
        }
        return string.CompareOrdinal(x, y);
    }

    private static int[] Parse(string? v)
    {
        if (string.IsNullOrEmpty(v)) return Array.Empty<int>();
        var parts = v.Split('.');
        var nums = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            nums[i] = int.TryParse(parts[i], out var n) ? n : -1; // snapshots/pre-releases sort lower
        return nums;
    }
}
