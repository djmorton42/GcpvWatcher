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
GcpvWatcher Calculator v${VERSION}
================================

This is a Windows desktop calculator application built with Avalonia UI.

System Requirements:
- Windows 10 or later
- No additional dependencies required (self-contained)

How to Run:
1. Double-click on GcpvWatcher.exe
2. Click "Calculate" to see 4 + 2 = 6
3. Click "Close" to exit

Features:
- Simple calculator with add functionality
- Clean, modern UI
- Independent theme (ignores system theme)
- No installation required

Built: $(date)
EOF

# Create a simple batch file to run the application
cat > run.bat << EOF
@echo off
echo Starting GcpvWatcher Calculator...
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
