using System.Text.Json;
using GcpvWatcher.App.Models;

namespace GcpvWatcher.App.Services;

/// <summary>
/// Persists per-race name and lap edits beside the FinishLynx event.
/// </summary>
public sealed class RaceOverrideStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _path;
    private readonly object _lock = new();
    private Dictionary<string, RaceOverride> _overrides = new(StringComparer.Ordinal);

    public RaceOverrideStore(string finishLynxDirectory)
    {
        if (string.IsNullOrWhiteSpace(finishLynxDirectory))
            throw new ArgumentException("FinishLynx directory is required.", nameof(finishLynxDirectory));

        _path = Path.Combine(finishLynxDirectory, "gcpv-race-overrides.json");
        Load();
    }

    public Race Apply(Race race)
    {
        lock (_lock)
        {
            if (!_overrides.TryGetValue(race.RaceNumber, out var edit) || edit.IsEmpty)
                return race;

            var lapsOverridden = edit.Laps.HasValue;
            return race with
            {
                RaceTitle = edit.Title ?? race.RaceTitle,
                NumberOfLaps = edit.Laps ?? race.NumberOfLaps,
                TrackWarning = lapsOverridden ? null : race.TrackWarning,
                TrackCaution = lapsOverridden ? null : race.TrackCaution,
                IsOverridden = true
            };
        }
    }

    public void SetTitle(string raceNumber, string? title)
    {
        lock (_lock)
        {
            var target = RaceOverrideTargets.TitleTarget(raceNumber);
            var edit = GetOrAdd(target);
            edit.Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
            RemoveIfEmpty(target);
            Save();
        }
    }

    public void SetLaps(IEnumerable<string> raceNumbers, decimal laps)
    {
        lock (_lock)
        {
            foreach (var raceNumber in raceNumbers)
            {
                foreach (var target in RaceOverrideTargets.LapTargets(raceNumber))
                {
                    var edit = GetOrAdd(target);
                    edit.Laps = laps;
                }
            }

            Save();
        }
    }

    public void Clear(IEnumerable<string> raceNumbers)
    {
        lock (_lock)
        {
            foreach (var raceNumber in raceNumbers)
            {
                foreach (var target in RaceOverrideTargets.ClearTargets(raceNumber))
                    _overrides.Remove(target);
            }

            Save();
        }
    }

    /// <summary>
    /// Drops edits whose race number is no longer present in the exports or keep file.
    /// </summary>
    public void Prune(IEnumerable<string> existingRaceNumbers)
    {
        lock (_lock)
        {
            var existing = existingRaceNumbers.ToHashSet(StringComparer.Ordinal);
            var removed = _overrides.Keys.Where(key => !existing.Contains(key)).ToList();
            if (removed.Count == 0)
                return;

            foreach (var key in removed)
                _overrides.Remove(key);

            Save();
        }
    }

    private RaceOverride GetOrAdd(string raceNumber)
    {
        if (!_overrides.TryGetValue(raceNumber, out var edit))
        {
            edit = new RaceOverride();
            _overrides[raceNumber] = edit;
        }

        return edit;
    }

    private void RemoveIfEmpty(string raceNumber)
    {
        if (_overrides.TryGetValue(raceNumber, out var edit) && edit.IsEmpty)
            _overrides.Remove(raceNumber);
    }

    private void Load()
    {
        if (!File.Exists(_path))
            return;

        try
        {
            var json = File.ReadAllText(_path);
            var loaded = JsonSerializer.Deserialize<Dictionary<string, RaceOverride>>(json);
            _overrides = loaded is null
                ? new Dictionary<string, RaceOverride>(StringComparer.Ordinal)
                : new Dictionary<string, RaceOverride>(loaded, StringComparer.Ordinal);
        }
        catch (Exception ex)
        {
            ApplicationLogger.LogException("Error loading race overrides", ex);
            _overrides = new Dictionary<string, RaceOverride>(StringComparer.Ordinal);
        }
    }

    private void Save()
    {
        try
        {
            if (_overrides.Count == 0)
            {
                if (File.Exists(_path))
                    File.Delete(_path);
                return;
            }

            var json = JsonSerializer.Serialize(_overrides, JsonOptions);
            File.WriteAllText(_path, json);
        }
        catch (Exception ex)
        {
            ApplicationLogger.LogException("Error saving race overrides", ex);
        }
    }
}
