using GcpvWatcher.App.Models;

namespace GcpvWatcher.App.ViewModels;

public sealed class TrackLengthChoice
{
    public TrackLengthChoice(ForcedTrackLength value, string label)
    {
        Value = value;
        Label = label;
    }

    public ForcedTrackLength Value { get; }

    public string Label { get; }

    public override string ToString() => Label;
}
