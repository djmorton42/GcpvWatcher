using System.Text.RegularExpressions;

namespace GcpvWatcher.App.Parsers;

/// <summary>
/// Detects and normalizes race advancement markers of the form "(x + m)".
/// Advancement may appear embedded in the stage text (e.g. "Heat, 2 + 4")
/// or as a separate CSV cell after the race number (e.g. "(5 + 0)").
/// </summary>
public static partial class AdvancementNormalizer
{
    [GeneratedRegex(@"(?:\(\s*)?(\d+)\s*\+\s*(\d+)(?:\s*\))?", RegexOptions.CultureInvariant)]
    private static partial Regex EmbeddedAdvancementRegex();

    [GeneratedRegex(@"^(?:\(\s*)?(\d+)\s*\+\s*(\d+)(?:\s*\))?$", RegexOptions.CultureInvariant)]
    private static partial Regex WholeCellAdvancementRegex();

    /// <summary>
    /// Returns a stage string with advancement normalized to "(x + m)".
    /// Prefers advancement already present in <paramref name="stage"/> over
    /// <paramref name="advancementCandidate"/> when both exist.
    /// Trailing commas before advancement (or with no advancement) are removed.
    /// </summary>
    public static string Normalize(string stage, string? advancementCandidate)
    {
        stage ??= string.Empty;

        if (TryFormatEmbedded(stage, out var normalizedFromStage))
            return normalizedFromStage;

        if (TryParseAdvancement(advancementCandidate, out var formatted))
            return JoinStageAndAdvancement(stage, formatted);

        return StripTrailingComma(stage);
    }

    public static bool TryParseAdvancement(string? value, out string formatted)
    {
        formatted = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var match = WholeCellAdvancementRegex().Match(value.Trim());
        if (!match.Success)
            return false;

        formatted = Format(match.Groups[1].Value, match.Groups[2].Value);
        return true;
    }

    private static bool TryFormatEmbedded(string stage, out string normalized)
    {
        var match = EmbeddedAdvancementRegex().Match(stage);
        if (!match.Success)
        {
            normalized = stage;
            return false;
        }

        var formatted = Format(match.Groups[1].Value, match.Groups[2].Value);
        var prefix = stage[..match.Index];
        var suffix = stage[(match.Index + match.Length)..];
        normalized = JoinStageAndAdvancement(prefix, formatted, suffix);
        return true;
    }

    private static string JoinStageAndAdvancement(string stagePrefix, string formatted, string suffix = "")
    {
        var cleaned = StripTrailingComma(stagePrefix);
        if (string.IsNullOrEmpty(cleaned))
            return $"{formatted}{suffix}".TrimEnd();

        return $"{cleaned} {formatted}{suffix}".TrimEnd();
    }

    private static string StripTrailingComma(string value)
    {
        var trimmed = value.TrimEnd();
        if (trimmed.EndsWith(','))
            trimmed = trimmed[..^1].TrimEnd();

        return trimmed;
    }

    private static string Format(string first, string second) => $"({first} + {second})";
}
