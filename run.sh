#!/bin/bash

# GcpvWatcher - Run Application Script
# This script builds and runs the Avalonia desktop application

set -e  # Exit on any error

echo "🚀 Starting GcpvWatcher Application..."
echo "=================================="

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

# Build the application
echo "📦 Building application..."
dotnet build --configuration Release

# Run the application
echo "▶️  Launching application..."
dotnet run --configuration Release
