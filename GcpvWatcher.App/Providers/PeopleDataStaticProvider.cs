namespace GcpvWatcher.App.Providers;

public class PeopleDataStaticProvider : BaseDataProvider
{
    private readonly IEnumerable<string> _data;

    public PeopleDataStaticProvider(IEnumerable<string> data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
    }

    public override Task<IEnumerable<string>> GetDataRowsAsync()
    {
        return Task.FromResult(FilterCommentLines(_data));
    }
}