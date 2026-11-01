using GcpvWatcher.App.Models;
using GcpvWatcher.App.Services;

namespace GcpvWatcher.Tests.Services;

public class KeepFileRacerIdentityTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly AppConfig _config;

    public KeepFileRacerIdentityTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
        _config = new AppConfig
        {
            OutputEncoding = "utf-8",
            EvtBackupDirectory = "backups"
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, true);
    }

    [Fact]
    public void NamedKeepRacer_IsWrittenWithLastNameFirstNameAndAffiliation()
    {
        WriteKeepFile(
            RaceHeader("21A"),
            ",1051,1,Smith,Jane,Hamilton");

        ReloadKeepRaces();

        Assert.Equal(new[] { ",1051,1,Smith,Jane,Hamilton" }, RacerLines());
    }

    [Fact]
    public void UnnamedKeepRacers_AreWrittenWithoutEmptyNameColumns()
    {
        WriteKeepFile(
            RaceHeader("21A"),
            ",1051,1",
            ",2010,2");

        ReloadKeepRaces();

        var racerLines = RacerLines();
        Assert.Equal(new[] { ",1051,1", ",2010,2" }, racerLines);
        Assert.DoesNotContain(racerLines, IsBlankNameRow);
    }

    [Fact]
    public void PartialKeepRacerName_KeepsLaterFieldsInTheirOwnColumn()
    {
        WriteKeepFile(
            RaceHeader("21A"),
            ",1051,1,Smith",
            ",2010,2,,Jane",
            ",681,3,,,Hamilton",
            ",563,4,Smith,,Hamilton",
            ",999,5");

        ReloadKeepRaces();

        var racerLines = RacerLines();
        Assert.Equal(
            new[]
            {
                ",1051,1,Smith",
                ",2010,2,,Jane",
                ",681,3,,,Hamilton",
                ",563,4,Smith,,Hamilton",
                ",999,5"
            },
            racerLines);
        Assert.DoesNotContain(racerLines, IsBlankNameRow);
    }

    [Fact]
    public void KeepRacerName_StaysWithThatSkaterWhenLinesAreOrderedByLane()
    {
        WriteKeepFile(
            RaceHeader("21A"),
            ",2010,2,Smith,Jane,Hamilton",
            ",1051,1,Porter,Reggie,Toronto");

        ReloadKeepRaces();

        Assert.Equal(
            new[]
            {
                ",1051,1,Porter,Reggie,Toronto",
                ",2010,2,Smith,Jane,Hamilton"
            },
            RacerLines());
    }

    [Fact]
    public void KeepFile_StillWritesNameWhenTheRacerRowHasFieldsAfterAffiliation()
    {
        WriteKeepFile(
            RaceHeader("21A"),
            ",1051,1,Smith,Jane,Hamilton,,LIC42");

        ReloadKeepRaces();

        Assert.Equal(new[] { ",1051,1,Smith,Jane,Hamilton" }, RacerLines());
    }

    [Fact]
    public void ConsolidatedSingleRacer_KeepsThatSkatersNameAtTheNewLane()
    {
        _config.ConsolidateSingleRacerRaces = true;
        WriteKeepFile(
            RaceHeader("15A"),
            ",100,1,Porter,Reggie,Hamilton",
            ",200,2",
            RaceHeader("15B"),
            ",300,1,Smith,Jane,Toronto",
            RaceHeader("15C"),
            ",301,1");

        ReloadKeepRaces();

        var lines = EvtLines();
        Assert.Contains(lines, line => line.StartsWith("15ABC,"));
        var racerLines = RacerLines(lines);
        Assert.Equal(
            new[]
            {
                ",100,1,Porter,Reggie,Hamilton",
                ",200,2",
                ",300,3,Smith,Jane,Toronto",
                ",301,4"
            },
            racerLines);
        Assert.DoesNotContain(racerLines, IsBlankNameRow);
    }

    [Fact]
    public void SplitCombinedKeepRace_KeepsEachSkatersName()
    {
        WriteKeepFile(
            RaceHeader("15AB"),
            ",100,1,Porter,Reggie,Hamilton",
            ",200,2,Smith,Jane,Toronto");

        ReloadKeepRaces();

        var lines = EvtLines();
        Assert.Contains(lines, line => line.StartsWith("15A,"));
        Assert.Contains(lines, line => line.StartsWith("15B,"));
        Assert.DoesNotContain(lines, line => line.StartsWith("15AB,"));
        Assert.Equal(
            new[]
            {
                ",100,1,Porter,Reggie,Hamilton",
                ",200,1,Smith,Jane,Toronto"
            },
            RacerLines(lines));
    }

    [Fact]
    public void CsvRacer_RemainsWithoutNameColumnsWhenAnotherRaceComesFromTheKeepFile()
    {
        WriteKeepFile(
            RaceHeader("21A"),
            ",1051,1,Smith,Jane,Hamilton");

        var csvRace = new Race("22A", "From CSV", 4m, new Dictionary<string, int>
        {
            ["400"] = 1,
            ["401"] = 2
        });

        using var manager = new EvtFileManager(_tempDirectory, _config);
        manager.UpdateRacesFromFile("race.csv", new[] { csvRace });

        var racerLines = RacerLines();
        Assert.Equal(
            new[]
            {
                ",1051,1,Smith,Jane,Hamilton",
                ",400,1",
                ",401,2"
            },
            racerLines);
        Assert.DoesNotContain(racerLines, IsBlankNameRow);
    }

    private void WriteKeepFile(params string[] lines)
    {
        File.WriteAllLines(Path.Combine(_tempDirectory, "Lynx.evt.keep"), lines);
    }

    private void ReloadKeepRaces()
    {
        using var manager = new EvtFileManager(_tempDirectory, _config);
        manager.ReloadKeepRaces();
    }

    private IReadOnlyList<string> EvtLines()
    {
        return File.ReadAllLines(Path.Combine(_tempDirectory, "Lynx.evt"))
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToList();
    }

    private IReadOnlyList<string> RacerLines() => RacerLines(EvtLines());

    private static IReadOnlyList<string> RacerLines(IEnumerable<string> lines)
    {
        return lines.Where(line => line.StartsWith(',')).ToList();
    }

    private static string RaceHeader(string raceNumber)
    {
        return $"{raceNumber},,,\"Final\",,,,,,,,,4";
    }

    private static bool IsBlankNameRow(string racerLine)
    {
        var fields = racerLine.Split(',');
        if (fields.Length <= 3)
            return false;

        var lastName = fields.Length > 3 ? fields[3] : "";
        var firstName = fields.Length > 4 ? fields[4] : "";
        var affiliation = fields.Length > 5 ? fields[5] : "";
        return lastName.Length == 0 && firstName.Length == 0 && affiliation.Length == 0;
    }
}
