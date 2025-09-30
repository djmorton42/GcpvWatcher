using GcpvWatcher.App.Models;
using GcpvWatcher.App.Services;
using System.Text.RegularExpressions;

namespace GcpvWatcher.App.Providers;

public class GcpvExportDataDirectoryProvider : BaseGcpvExportDataProvider
{
    private readonly string _directoryPath;
    private readonly string _filePattern;
    private readonly AppConfigService _configService;

    public GcpvExportDataDirectoryProvider(string directoryPath, AppConfigService configService)
    {
        _directoryPath = directoryPath ?? throw new ArgumentNullException(nameof(directoryPath));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));

        if (!Directory.Exists(_directoryPath))
            throw new DirectoryNotFoundException($"Directory '{_directoryPath}' not found.");

        // Load config to get file pattern
        var config = _configService.LoadConfig();
        _filePattern = config.GcpvExportFilePattern;

        if (string.IsNullOrWhiteSpace(_filePattern))
            throw new InvalidOperationException("GcpvExportFilePattern not found in configuration.");
    }

    public override async Task<IEnumerable<string>> GetDataRowsAsync()
    {
        // Find files matching the pattern
        var matchingFiles = Directory.GetFiles(_directoryPath, _filePattern);
        
        if (matchingFiles.Length == 0)
            throw new FileNotFoundException($"No files found matching pattern '{_filePattern}' in directory '{_directoryPath}'.");

        var allRows = new List<string>();

        foreach (var filePath in matchingFiles)
        {
            var lines = await File.ReadAllLinesAsync(filePath);
            allRows.AddRange(lines);
        }

        // Use base class filtering for blank lines only
        return FilterBlankLines(allRows);
    }
}
