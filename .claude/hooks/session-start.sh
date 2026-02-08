#!/bin/bash
set -euo pipefail

# Only run in remote/web environments
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

# Install .NET 8.0 SDK if not already installed
if ! command -v dotnet &> /dev/null || ! dotnet --list-sdks 2>/dev/null | grep -q "^8\."; then
  echo "Installing .NET 8.0 SDK..."
  apt-get update -qq
  apt-get install -y -qq dotnet-sdk-8.0
  echo ".NET 8.0 SDK installed: $(dotnet --version)"
else
  echo ".NET 8.0 SDK already installed: $(dotnet --version)"
fi

# Restore NuGet packages if possible
PROJECT_FILE="$CLAUDE_PROJECT_DIR/ReportingSystem/ReportingSystem.csproj"
if [ -f "$PROJECT_FILE" ]; then
  echo "Restoring NuGet packages..."
  dotnet restore "$PROJECT_FILE" 2>&1 || echo "Warning: NuGet restore failed (nuget.org may be unreachable in this environment)"
fi
