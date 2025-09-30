using System.IO;

namespace GcpvWatcher.App.Providers;

public class GcpvExportDataFileProvider : BaseGcpvExportDataProvider
{
    private readonly string _filePath;

    public GcpvExportDataFileProvider(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        if (!filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("File must have a .csv extension.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"The file '{filePath}' was not found.");

        _filePath = filePath;
    }

    public override async Task<IEnumerable<string>> GetDataRowsAsync()
    {
        // Defensive check - file might have been deleted/moved since construction
        if (!File.Exists(_filePath))
            throw new FileNotFoundException($"The file '{_filePath}' was not found.");

        var lines = await File.ReadAllLinesAsync(_filePath);
        
        // Use base class filtering for blank lines only
        return FilterBlankLines(lines);
    }
}
