using System.Text;
using GcpvWatcher.App.Models;
using GcpvWatcher.App.Services;
using Xunit;

namespace GcpvWatcher.Tests.Services;

public class EvtFileLineTerminatorTests
{
    [Fact]
    public void LineTerminators_AreConsistentAcrossPlatforms()
    {
        // This test verifies that we're using Environment.NewLine consistently
        var expectedNewLine = Environment.NewLine;
        
        // Verify that Environment.NewLine is what we expect on this platform
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            Assert.Equal("\r\n", expectedNewLine);
        }
        else
        {
            Assert.Equal("\n", expectedNewLine);
        }
    }

    [Fact]
    public void StringWriter_UsesSystemDefaultLineTerminators()
    {
        // Test that StringWriter (used by CsvHelper) uses system default line terminators
        using var writer = new StringWriter();
        writer.WriteLine("Test line 1");
        writer.WriteLine("Test line 2");
        
        var content = writer.ToString();
        
        // Verify that StringWriter uses Environment.NewLine
        Assert.Contains(Environment.NewLine, content);
        
        // Verify that we have the expected number of line terminators
        var expectedNewLineCount = 2; // Two WriteLine calls
        var actualNewLineCount = content.Split(Environment.NewLine).Length - 1;
        Assert.Equal(expectedNewLineCount, actualNewLineCount);
    }

    [Fact]
    public void CsvHelper_WithStringWriter_UsesSystemDefaultLineTerminators()
    {
        // Test that CsvHelper with StringWriter produces correct line terminators
        using var writer = new StringWriter();
        using var csv = new CsvHelper.CsvWriter(writer, new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            TrimOptions = CsvHelper.Configuration.TrimOptions.None
        });

        // Write a test record
        csv.WriteField("field1");
        csv.WriteField("field2");
        csv.NextRecord();
        
        var content = writer.ToString();
        
        // Verify that CsvHelper with StringWriter uses Environment.NewLine
        Assert.Contains(Environment.NewLine, content);
        
        // Verify that the content ends with the system's default line terminator
        Assert.EndsWith(Environment.NewLine, content);
    }
}
