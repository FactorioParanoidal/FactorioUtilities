using FluentAssertions;
using FactorioParanoidal.FactorioMods;

namespace FactorioParanoidal.FactorioMods.Tests;

public class NaturalSortTests
{
    [Theory]
    [InlineData("a", "b", -1)]
    [InlineData("b", "a", 1)]
    [InlineData("a", "a", 0)]
    [InlineData("a1", "a2", -1)]
    [InlineData("a10", "a2", 1)]
    [InlineData("a2", "a10", -1)]
    [InlineData("a01", "a1", -1)]
    [InlineData("a1", "a01", 1)]
    [InlineData("01", "012", -1)]
    [InlineData("12", "9", 1)]
    [InlineData("mod-1", "mod-01", 1)]
    [InlineData("mod-01", "mod-1", -1)]
    [InlineData("mod-1.1", "mod-1.10", -1)]
    [InlineData("mod-1.10", "mod-1.2", 1)]
    [InlineData("A", "a", 0)] // foldCase = true by default in my calls
    public void StrNatCmp0_ShouldMatchExpectedBehavior(string a, string b, int expectedSign)
    {
        int result = FactorioModpack.StrNatCmp0(a, b, true);
        Math.Sign(result).Should().Be(expectedSign);
    }

    [Fact]
    public void StrNatCmp0_CaseSensitivityTest()
    {
        FactorioModpack.StrNatCmp0("A", "a", false).Should().BeNegative();
        FactorioModpack.StrNatCmp0("a", "A", true).Should().Be(0);
    }
}
