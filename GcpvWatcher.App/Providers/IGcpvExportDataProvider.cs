namespace GcpvWatcher.App.Providers;

public interface IGcpvExportDataProvider
{
    Task<IEnumerable<string>> GetDataRowsAsync();
}
