using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Data.Converters;
using GcpvWatcher.App.Models;
using GcpvWatcher.App.Services;

namespace GcpvWatcher.App.Converters;

public class RacersToStringConverter : IValueConverter
{
    // |id|id|...| with one or more ids, each 1 to 5 digits, and a pipe at both ends.
    private static readonly Regex RelayMemberIdsPattern = new(
        @"^(\|[0-9]{1,5})+\|$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            Race race => Format(race.Racers, race.RacerNames),
            Dictionary<string, int> racers => Format(racers, null),
            _ => "No racers"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static string Format(Dictionary<string, int> racers, IReadOnlyDictionary<string, KeepRacerName>? keepNames)
    {
        if (racers.Count == 0)
            return "No racers";

        var racerData = RacerDataService.GetRacers();
        var racerIdWidth = Math.Max(racers.Keys.DefaultIfEmpty("").Max(id => id.Length), 4);
        var lines = new List<string>();

        foreach (var kvp in racers.OrderBy(entry => entry.Value))
        {
            var racerId = kvp.Key;
            var paddedRacerId = racerId.PadLeft(racerIdWidth);
            var lanePrefix = $"Lane {kvp.Value,2}, {paddedRacerId}";

            if (keepNames != null &&
                keepNames.TryGetValue(racerId, out var keepName) &&
                TryParseRelayMemberIds(keepName.FirstName, out var memberIds))
            {
                var teamLabel = FormatTeamLabel(keepName);
                lines.Add(teamLabel == null ? lanePrefix : $"{lanePrefix} - {teamLabel}");
                var indent = new string(' ', lanePrefix.Length + (teamLabel == null ? 1 : 3));
                foreach (var memberId in memberIds)
                    lines.Add(indent + (LookupPersonName(racerData, memberId) ?? memberId));
            }
            else
            {
                lines.Add(FormatIndividual(lanePrefix, racerData, racerId));
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static bool TryParseRelayMemberIds(string? firstName, out IReadOnlyList<string> memberIds)
    {
        memberIds = Array.Empty<string>();
        if (string.IsNullOrWhiteSpace(firstName))
            return false;

        var trimmed = firstName.Trim();
        if (!RelayMemberIdsPattern.IsMatch(trimmed))
            return false;

        memberIds = trimmed.Split('|', StringSplitOptions.RemoveEmptyEntries);
        return memberIds.Count > 0;
    }

    private static string? FormatTeamLabel(KeepRacerName name)
    {
        var teamName = name.LastName.Trim();
        var affiliation = name.Affiliation.Trim();
        if (teamName.Length == 0 && affiliation.Length == 0)
            return null;
        if (teamName.Length == 0)
            return affiliation;
        if (affiliation.Length == 0)
            return teamName;
        return $"{teamName} ({affiliation})";
    }

    private static string? LookupPersonName(IReadOnlyDictionary<string, Racer>? racerData, string racerId)
    {
        if (racerData == null || !racerData.TryGetValue(racerId, out var racer))
            return null;

        var nameParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(racer.FirstName))
            nameParts.Add(racer.FirstName.Trim());
        if (!string.IsNullOrWhiteSpace(racer.LastName))
            nameParts.Add(racer.LastName.Trim());
        return nameParts.Count == 0 ? null : string.Join(" ", nameParts);
    }

    private static string FormatIndividual(string lanePrefix, IReadOnlyDictionary<string, Racer>? racerData, string racerId)
    {
        if (racerData != null && racerData.TryGetValue(racerId, out var racer))
        {
            var nameParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(racer.FirstName))
                nameParts.Add(racer.FirstName);
            if (!string.IsNullOrWhiteSpace(racer.LastName))
                nameParts.Add(racer.LastName);
            var namePart = nameParts.Count > 0 ? string.Join(" ", nameParts) : null;
            var affiliationPart = !string.IsNullOrWhiteSpace(racer.Affiliation)
                ? $" ({racer.Affiliation})"
                : string.Empty;

            if (namePart != null)
                return $"{lanePrefix} - {namePart}{affiliationPart}";
            if (!string.IsNullOrWhiteSpace(racer.Affiliation))
                return $"{lanePrefix} - {racer.Affiliation}";
        }

        return lanePrefix;
    }
}
