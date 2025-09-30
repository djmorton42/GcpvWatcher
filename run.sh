#!/bin/bash

# GcpvWatcher - Run Application Script
# This script builds and runs the Avalonia desktop application

set -e  # Exit on any error

echo "🚀 Starting GcpvWatcher Application..."
echo "=================================="

# Change to the application directory
cd "$(dirname "$0")/GcpvWatcher.App"

# Build the application
echo "📦 Building application..."
dotnet build --configuration Release

# Run the application
echo "▶️  Launching application..."
dotnet run --configuration Release
