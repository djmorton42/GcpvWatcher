using GcpvWatcher.App.Models;
using GcpvWatcher.App.Services;
using Xunit;

namespace GcpvWatcher.Tests.Services;

public class RaceOverrideStoreTests : IDisposable
{
    private readonly string _directory;

    public RaceOverrideStoreTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }

    [Fact]
    public void Apply_WithLapOverride_ReplacesLapsAndClearsNotices()
    {
        var store = new RaceOverrideStore(_directory);
        store.SetLaps(["38A"], 9m);

        var race = new Race("38A", "All Skaters 1000m Final", 10m, new Dictionary<string, int>())
        {
            TrackWarning = "bad laps",
            TrackCaution = "file says 100 m"
        };

        var applied = store.Apply(race);

        Assert.Equal(9m, applied.NumberOfLaps);
        Assert.Equal("All Skaters 1000m Final", applied.RaceTitle);
        Assert.Null(applied.TrackWarning);
        Assert.Null(applied.TrackCaution);
        Assert.True(applied.IsOverridden);
    }

    [Fact]
    public void Apply_WithTitleOverride_KeepsCalculatedLapsAndNotices()
    {
        var store = new RaceOverrideStore(_directory);
        store.SetTitle("38A", "Open Men 1000m Final");

        var race = new Race("38A", "All Skaters 1000m Final", 9m, new Dictionary<string, int>())
        {
            TrackCaution = "File says 100 m."
        };

        var applied = store.Apply(race);

        Assert.Equal("Open Men 1000m Final", applied.RaceTitle);
        Assert.Equal(9m, applied.NumberOfLaps);
        Assert.Equal("File says 100 m.", applied.TrackCaution);
        Assert.True(applied.IsOverridden);
    }

    [Fact]
    public void SetLaps_OnConsolidatedNumber_StoresEachSourceRace()
    {
        var store = new RaceOverrideStore(_directory);
        store.SetLaps(["37CD"], 13.5m);

        var first = store.Apply(new Race("37C", "Open Men 1500m Heat", 15m, new Dictionary<string, int>()));
        var second = store.Apply(new Race("37D", "Open Men 1500m Heat", 15m, new Dictionary<string, int>()));

        Assert.Equal(13.5m, first.NumberOfLaps);
        Assert.Equal(13.5m, second.NumberOfLaps);
    }

    [Fact]
    public void Prune_RemovesOverridesForRacesThatAreGone()
    {
        var store = new RaceOverrideStore(_directory);
        store.SetLaps(["38A", "39A"], 4m);

        store.Prune(["38A"]);

        var reloaded = new RaceOverrideStore(_directory);
        var kept = reloaded.Apply(new Race("38A", "Title", 10m, new Dictionary<string, int>()));
        var dropped = reloaded.Apply(new Race("39A", "Title", 10m, new Dictionary<string, int>()));

        Assert.Equal(4m, kept.NumberOfLaps);
        Assert.Equal(10m, dropped.NumberOfLaps);
        Assert.False(dropped.IsOverridden);
    }

    [Fact]
    public void Clear_RemovesTheEditSoTheCalculatedRaceReturns()
    {
        var store = new RaceOverrideStore(_directory);
        store.SetTitle("38A", "Custom");
        store.SetLaps(["38A"], 4m);
        store.Clear(["38A"]);

        var applied = store.Apply(new Race("38A", "Calculated", 9m, new Dictionary<string, int>()));

        Assert.Equal("Calculated", applied.RaceTitle);
        Assert.Equal(9m, applied.NumberOfLaps);
        Assert.False(applied.IsOverridden);
    }

    [Fact]
    public void GetRacesForDisplay_UsesSavedLaps()
    {
        var manager = new EvtFileManager(_directory, new AppConfig());
        manager.SetExistingRaces(
        [
            new Race("38A", "All Skaters 1000m Final", 10m, new Dictionary<string, int> { ["1"] = 1 })
            {
                TrackWarning = "not a whole or half lap"
            }
        ]);
        manager.SetRaceLaps(["38A"], 9m);

        var race = manager.GetRacesForDisplay().Single();

        Assert.Equal(9m, race.NumberOfLaps);
        Assert.Null(race.TrackWarning);
        Assert.True(race.IsOverridden);
    }
}
