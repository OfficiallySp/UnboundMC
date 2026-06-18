using System.Security.Cryptography;

namespace Unbound.Core;

public static class Hashing
{
    public static string Sha512Hex(ReadOnlySpan<byte> data) => Convert.ToHexStringLower(SHA512.HashData(data));
    public static string Sha1Hex(ReadOnlySpan<byte> data) => Convert.ToHexStringLower(SHA1.HashData(data));

    /// <summary>True if the data matches the expected hex digest. A null/empty expectation passes (nothing to check).</summary>
    public static bool VerifySha512(ReadOnlySpan<byte> data, string? expectedHex)
        => string.IsNullOrEmpty(expectedHex)
           || string.Equals(Sha512Hex(data), expectedHex, StringComparison.OrdinalIgnoreCase);
}
