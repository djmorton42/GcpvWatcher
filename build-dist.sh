#!/bin/bash

# GcpvWatcher - Build Distribution Script
# This script creates a Windows distribution package

set -e  # Exit on any error

echo "📦 Building GcpvWatcher Distribution..."
echo "====================================="

# Configuration
APP_NAME="GcpvWatcher"
VERSION="1.0.0"
DIST_DIR="dist"
WINDOWS_RUNTIME="win-x64"
ZIP_NAME="${APP_NAME}-v${VERSION}-${WINDOWS_RUNTIME}.zip"

# Clean previous builds
echo "🧹 Cleaning previous builds..."
rm -rf "$DIST_DIR"
mkdir -p "$DIST_DIR"

# Change to the application directory
cd "$(dirname "$0")/GcpvWatcher.App"

# Copy configuration file to application directory
echo "📋 Copying configuration file..."
if [ -f "../appconfig.json" ]; then
    cp "../appconfig.json" "appconfig.json"
    echo "✅ Configuration file copied successfully"
else
    echo "❌ Warning: appconfig.json not found in project root"
fi

# Build for Windows x64 with self-contained deployment
echo "🔨 Building for Windows x64..."
dotnet publish \
    --configuration Release \
    --runtime "$WINDOWS_RUNTIME" \
    --self-contained true \
    --output "../$DIST_DIR/$APP_NAME" \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=true \
    -p:IncludeNativeLibrariesForSelfExtract=true

# Create additional distribution files
echo "📝 Creating distribution files..."
cd "../$DIST_DIR"

# Create a README for the distribution
cat > README.txt << EOF
GcpvWatcher v${VERSION}
=====================

This is a Windows desktop file watcher application for GCPV export files built with Avalonia UI.

System Requirements:
- Windows 10 or later
- No additional dependencies required (self-contained)

How to Run:
1. Double-click on GcpvWatcher.exe
2. Select the watch directory (where GCPV export files are placed)
3. Select the FinishLynx directory (where Lynx.evt file is located)
4. Click "Start Watching" to begin monitoring
5. The application will automatically process files matching the pattern

Features:
- Real-time file monitoring for GCPV export files
- Automatic race data extraction and conversion
- EVT file generation for FinishLynx
- Clean, modern UI
- No installation required

Built: $(date)
EOF

# Create a simple batch file to run the application
cat > run.bat << EOF
@echo off
echo Starting GcpvWatcher...
GcpvWatcher.exe
pause
EOF

# Create the zip file
echo "📦 Creating distribution package..."
cd ..
zip -r "$ZIP_NAME" "$DIST_DIR"

# Display results
echo ""
echo "✅ Distribution created successfully!"
echo "📁 Package: $ZIP_NAME"
echo "📊 Size: $(du -h "$ZIP_NAME" | cut -f1)"
echo ""
echo "📋 Contents:"
ls -la "$DIST_DIR"
echo ""
echo "🚀 To test the distribution:"
echo "   1. Extract $ZIP_NAME"
echo "   2. Navigate to the extracted folder"
echo "   3. Double-click GcpvWatcher.exe or run.bat"
