#!/bin/bash

# GcpvWatcher - Run Tests Script
# This script runs all unit tests for the application

set -e  # Exit on any error

echo "🧪 Running GcpvWatcher Tests..."
echo "=============================="

# Change to the tests directory
cd "$(dirname "$0")/GcpvWatcher.Tests"

# Run all tests with verbose output
echo "📋 Running unit tests..."
dotnet test --configuration Release --verbosity normal

echo ""
echo "✅ All tests completed successfully!"
