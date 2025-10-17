using System.Text;
using GcpvWatcher.App.Models;
using GcpvWatcher.App.Services;
using Xunit;

namespace GcpvWatcher.Tests.Services;

public class AppConfigServiceTests
{
    [Theory]
    [InlineData("ascii", "US-ASCII")]
    [InlineData("ASCII", "US-ASCII")]
    [InlineData("utf-8", "Unicode (UTF-8)")]
    [InlineData("UTF-8", "Unicode (UTF-8)")]
    [InlineData("utf-16", "Unicode")]
    [InlineData("UTF-16", "Unicode")]
    public void GetOutputEncoding_ReturnsCorrectEncoding(string input, string expectedEncodingName)
    {
        // Act
        var encoding = AppConfigService.GetOutputEncoding(input);
        
        // Assert
        Assert.Equal(expectedEncodingName, encoding.EncodingName);
    }

    [Fact]
    public void GetOutputEncoding_WithInvalidInput_ReturnsAsciiAsDefault()
    {
        // Act
        var encoding = AppConfigService.GetOutputEncoding("invalid-encoding");
        
        // Assert
        Assert.Equal("US-ASCII", encoding.EncodingName);
    }

    [Fact]
    public void GetOutputEncoding_WithNullInput_ReturnsAsciiAsDefault()
    {
        // Act
        var encoding = AppConfigService.GetOutputEncoding(null!);
        
        // Assert
        Assert.Equal("US-ASCII", encoding.EncodingName);
    }

    [Fact]
    public void GetOutputEncoding_WithEmptyInput_ReturnsAsciiAsDefault()
    {
        // Act
        var encoding = AppConfigService.GetOutputEncoding("");
        
        // Assert
        Assert.Equal("US-ASCII", encoding.EncodingName);
    }
}
