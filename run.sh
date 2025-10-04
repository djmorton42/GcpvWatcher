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

# Build the application
echo "📦 Building application..."
dotnet build --configuration Release

# Run the application
echo "▶️  Launching application..."
dotnet run --configuration Release
