#!/bin/bash

# GcpvWatcher - Build Distribution Script
# This script creates a Windows distribution package

set -e  # Exit on any error

echo "📦 Building GcpvWatcher Distribution..."
echo "====================================="

# Configuration
APP_NAME="GcpvWatcher"
DIST_DIR="dist"
WINDOWS_RUNTIME="win-x64"
ZIP_NAME="${APP_NAME}-${WINDOWS_RUNTIME}.zip"

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

# Copy notification sound file to application directory
echo "🔊 Copying notification sound file..."
if [ -f "../etc/notification.mp3" ]; then
    mkdir -p "etc"
    cp "../etc/notification.mp3" "etc/notification.mp3"
    echo "✅ Notification sound file copied successfully"
    
    # Also copy to the output directory where the executable runs
    mkdir -p "bin/Release/net9.0/etc"
    cp "../etc/notification.mp3" "bin/Release/net9.0/etc/notification.mp3"
    echo "✅ Notification sound file copied to output directory"
else
    echo "❌ Warning: etc/notification.mp3 not found in project root"
fi

# Build for Windows x64 with self-contained deployment
echo "🔨 Building for Windows x64..."
dotnet publish \
    --configuration Release \
    --runtime "$WINDOWS_RUNTIME" \
    --self-contained true \
    --output "../$DIST_DIR" \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:AssemblyName=GcpvWatcher

# Copy additional files to distribution directory
echo "📝 Copying additional files to distribution..."
cd "../$DIST_DIR"

# Remove PDB file (not needed for distribution)
echo "🧹 Removing debug symbols..."
rm -f *.pdb
echo "✅ Debug symbols removed"

# Copy configuration and notification files to the distribution root
if [ -f "../GcpvWatcher.App/appconfig.json" ]; then
    cp "../GcpvWatcher.App/appconfig.json" "appconfig.json"
    echo "✅ Configuration file copied to distribution root"
fi

if [ -f "../GcpvWatcher.App/etc/notification.mp3" ]; then
    mkdir -p "etc"
    cp "../GcpvWatcher.App/etc/notification.mp3" "etc/notification.mp3"
    echo "✅ Notification sound file copied to distribution root"
fi

# README files are excluded from distribution

# No batch file needed - executable can be run directly

# Create the zip file
echo "📦 Creating distribution package..."
cd ..
cd "$DIST_DIR"
zip -r "../$ZIP_NAME" . -x "README*"
cd ..

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
echo "   3. Double-click GcpvWatcher.exe"
