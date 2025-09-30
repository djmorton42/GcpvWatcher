namespace GcpvWatcher.App.Providers;

public interface IEventDataProvider
{
    Task<IEnumerable<string>> GetDataRowsAsync();
}
