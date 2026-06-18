using Unbound.Core;

namespace Unbound.Core.Tests;

public class MavenTests
{
    [Theory]
    [InlineData("net.fabricmc:fabric-loader:0.16.10",
                "net/fabricmc/fabric-loader/0.16.10/fabric-loader-0.16.10.jar")]
    [InlineData("org.ow2.asm:asm:9.7",
                "org/ow2/asm/asm/9.7/asm-9.7.jar")]
    public void CoordinateToPath_buildsMavenLayout(string coord, string expected)
        => Assert.Equal(expected, Maven.CoordinateToPath(coord));

    [Fact]
    public void CoordinateToPath_handlesClassifier()
        => Assert.Equal(
            "net/fabricmc/sponge-mixin/0.13.4/sponge-mixin-0.13.4-sources.jar",
            Maven.CoordinateToPath("net.fabricmc:sponge-mixin:0.13.4:sources"));

    [Fact]
    public void ToUrl_joinsBaseAndPath_withSingleSlash()
        => Assert.Equal(
            "https://maven.fabricmc.net/net/fabricmc/fabric-loader/0.16.10/fabric-loader-0.16.10.jar",
            Maven.ToUrl("https://maven.fabricmc.net/", "net.fabricmc:fabric-loader:0.16.10"));

    [Fact]
    public void CoordinateToPath_rejectsBadCoordinate()
        => Assert.Throws<FormatException>(() => Maven.CoordinateToPath("not-a-coordinate"));
}
