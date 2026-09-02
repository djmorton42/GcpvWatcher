using GcpvWatcher.App.Parsers;
using Xunit;

namespace GcpvWatcher.Tests.Parsers;

public class AdvancementNormalizerTests
{
    [Theory]
    [InlineData("Heat, 2 + 4", null, "Heat (2 + 4)")]
    [InlineData("Heat, 2 +2", null, "Heat (2 + 2)")]
    [InlineData("Heat,2+4", null, "Heat (2 + 4)")]
    [InlineData("Heat (2 + 4)", null, "Heat (2 + 4)")]
    [InlineData("Semi-Final, ", "(5 + 0)", "Semi-Final (5 + 0)")]
    [InlineData("Semi-Final,", "5 + 0", "Semi-Final (5 + 0)")]
    [InlineData("Semi-Final", "(5+0)", "Semi-Final (5 + 0)")]
    [InlineData("Semi-Final, ", null, "Semi-Final")]
    [InlineData("Final", null, "Final")]
    [InlineData("Final", "Lane", "Final")]
    [InlineData("Final", "", "Final")]
    [InlineData("Heat 1", null, "Heat 1")]
    [InlineData("Heat, 2 + 4", "(5 + 0)", "Heat (2 + 4)")] // prefer stage over column
    [InlineData("Heat, 2 +2 with # and ;", null, "Heat (2 + 2) with # and ;")]
    public void Normalize_ProducesConsistentAdvancementFormat(string stage, string? candidate, string expected)
    {
        Assert.Equal(expected, AdvancementNormalizer.Normalize(stage, candidate));
    }

    [Theory]
    [InlineData("(5 + 0)", true, "(5 + 0)")]
    [InlineData("5 + 0", true, "(5 + 0)")]
    [InlineData("  ( 5  +  0 )  ", true, "(5 + 0)")]
    [InlineData("2+4", true, "(2 + 4)")]
    [InlineData("Lane", false, "")]
    [InlineData("", false, "")]
    [InlineData(null, false, "")]
    [InlineData("Heat, 2 + 4", false, "")]
    public void TryParseAdvancement_OnlyMatchesWholeCell(string? value, bool expectedSuccess, string expectedFormatted)
    {
        var success = AdvancementNormalizer.TryParseAdvancement(value, out var formatted);
        Assert.Equal(expectedSuccess, success);
        Assert.Equal(expectedFormatted, formatted);
    }
}
