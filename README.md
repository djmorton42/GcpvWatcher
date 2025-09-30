# GcpvWatcher

A program to watch a directory for exports from GCPV and auto-maintain a FinishLynx EVT file with the exported data.

## Features


## Quick Start

### Prerequisites
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

## Project Structure

```
GcpvWatcher/
├── GcpvWatcher.App/           # Main Avalonia application
│   ├── Services/              # CalculatorService
│   ├── Views/                 # MainWindow UI
│   └── ViewModels/            # MainWindowViewModel
├── GcpvWatcher.Tests/         # Unit tests
├── run.sh                     # Run application script
├── test.sh                    # Run tests script
└── build-dist.sh              # Build Windows distribution script
```

## Manual Commands

If you prefer to run commands manually:

```bash
# Run the application
cd GcpvWatcher.App && dotnet run

# Run tests
cd GcpvWatcher.Tests && dotnet test

# Build for Windows distribution
cd GcpvWatcher.App && dotnet publish -r win-x64 --self-contained -c Release
```

The `build-dist.sh` script creates a self-contained Windows executable that includes:
- All necessary dependencies
- README.txt with usage instructions
- run.bat for easy execution
- Single-file deployment for easy distribution