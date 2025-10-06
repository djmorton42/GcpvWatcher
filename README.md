# GcpvWatcher

A GUI application to watch a directory for CSV files matching a specific pattern, exported from Gcpv,
extract any race data from them, and update a Lynx.evt file to allow the races to be used in FinishLynx.

When watching is enabled, the Lynx.evt file will be updated automatically whenever a new CSV file is found.

The appconfig.json file allows for configuring the application's CSV parsing, including the filename
pattern to watch for, and the key fields to look for when parsing, and suffixes to automatically
remove from extracted values to improve readability.  Gcpv CSV exports are non-standard
CSV files, so we can configure the key values to look for in order to determine which columns each
value can be found in.

Sample Config:

```
{
  "GcpvExportFilePattern": "Race*.csv",
  "NotificationSoundPath": "etc/notification.mp3",
  "KeyFields": {
    "track_params": {
      "key": "Event :",
      "offset": 1
    },
    "race_group": {
      "key": "Event :",
      "offset": 2,
      "suffix_stop_words": ["male", "female", "Genders Mixed"]
    },
    "stage": {
      "key": "Stage :",
      "offset": 1
    },
    "race_number": {
      "key": "Race",
      "offset": 1
    },
    "lane": {
      "key": "Lane",
      "offset": 3
    },
    "racer": {
      "key": "Skaters",
      "offset": 3
    },
    "affiliation": {
      "key": "Club",
      "offset": 3
    }
  }
}
```

### Developing
- .NET 9.0 SDK
- macOS, Windows, or Linux

### Running the Application
```bash
./run.sh
```

### Running Tests
```bash
./test.sh
```

### Building Windows Distribution
```bash
./build-dist.sh
```
