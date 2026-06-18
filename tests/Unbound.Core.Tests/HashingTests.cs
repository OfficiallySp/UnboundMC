using System.Text;
using Unbound.Core;

namespace Unbound.Core.Tests;

public class HashingTests
{
    // Known SHA-512 of "abc".
    private const string AbcSha512 =
        "ddaf35a193617abacc417349ae20413112e6fa4e89a97ea20a9eeee64b55d39a" +
        "2192992a274fc1a836ba3c23a3feebbd454d4423643ce80e2a9ac94fa54ca49f";

    [Fact]
    public void Sha512Hex_matchesKnownVector()
        => Assert.Equal(AbcSha512, Hashing.Sha512Hex(Encoding.ASCII.GetBytes("abc")));

    [Fact]
    public void VerifySha512_passesOnMatch_caseInsensitive()
        => Assert.True(Hashing.VerifySha512(Encoding.ASCII.GetBytes("abc"), AbcSha512.ToUpperInvariant()));

    [Fact]
    public void VerifySha512_failsOnCorruptedData()
        => Assert.False(Hashing.VerifySha512(Encoding.ASCII.GetBytes("abd"), AbcSha512));

    [Fact]
    public void VerifySha512_passesWhenNoExpectationGiven()
        => Assert.True(Hashing.VerifySha512(Encoding.ASCII.GetBytes("anything"), null));
}
