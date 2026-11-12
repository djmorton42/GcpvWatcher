using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using GcpvWatcher.App.Models;

namespace GcpvWatcher.App.ViewModels;

public class RaceRowViewModel : INotifyPropertyChanged
{
    private readonly Action _selectionChanged;
    private bool _isSelected;
    private string _titleText;
    private string _lapsText;

    public RaceRowViewModel(Race race, bool isSelected, Action selectionChanged, Action<RaceRowViewModel> save, Action<RaceRowViewModel> reset)
    {
        Race = race;
        _isSelected = isSelected;
        _selectionChanged = selectionChanged;
        _titleText = race.RaceTitle;
        _lapsText = race.NumberOfLaps.ToString(CultureInfo.InvariantCulture);
        SaveCommand = new RelayCommand(() => save(this));
        ResetCommand = new RelayCommand(() => reset(this));
    }

    public Race Race { get; }

    public string RaceNumber => Race.RaceNumber;

    public bool IsFromKeepFile => Race.IsFromKeepFile;

    public bool IsOverridden => Race.IsOverridden;

    public string? TrackWarning => Race.TrackWarning;

    public bool HasTrackWarning => Race.HasTrackWarning;

    public string? TrackCaution => Race.TrackCaution;

    public bool HasTrackCaution => Race.HasTrackCaution;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;
            _isSelected = value;
            OnPropertyChanged();
            _selectionChanged();
        }
    }

    public string TitleText
    {
        get => _titleText;
        set
        {
            if (_titleText == value)
                return;
            _titleText = value;
            OnPropertyChanged();
        }
    }

    public string LapsText
    {
        get => _lapsText;
        set
        {
            if (_lapsText == value)
                return;
            _lapsText = value;
            OnPropertyChanged();
        }
    }

    public ICommand SaveCommand { get; }

    public ICommand ResetCommand { get; }

    public bool TryReadLaps(out decimal laps)
    {
        return decimal.TryParse(LapsText.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out laps) && laps >= 0;
    }

    public bool TitleChanged => !string.Equals(TitleText.Trim(), Race.RaceTitle, StringComparison.Ordinal);

    public bool LapsChanged => TryReadLaps(out var laps) && laps != Race.NumberOfLaps;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
