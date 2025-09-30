namespace GcpvWatcher.App.Providers;

public interface IPeopleDataProvider
{
    Task<IEnumerable<string>> GetDataRowsAsync();
}
