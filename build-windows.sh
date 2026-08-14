#!/bin/bash
# Build script for Windows
# Run on Windows (Git Bash or WSL) to build the launcher

set -e

echo "=== Building DSH Launcher for Windows ==="

# Check .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET SDK not found. Install from: https://dotnet.microsoft.com/download"
    exit 1
fi

# Build x64
echo "Building x64..."
cd DshLauncher.Windows
dotnet publish -c Release -r win-x64 --self-contained false -o ../dist/windows-x64
cp start-dsh.vbs ../dist/windows-x64/
cp ../assets/icon.ico ../dist/windows-x64/
cd ..

# Build arm64
echo "Building arm64..."
cd DshLauncher.Windows
dotnet publish -c Release -r win-arm64 --self-contained false -o ../dist/windows-arm64
cp start-dsh.vbs ../dist/windows-arm64/
cp ../assets/icon.ico ../dist/windows-arm64/
cd ..

# Package
tar -czf dist/dsh-launcher-windows-x64.tar.gz -C dist/windows-x64 .
tar -czf dist/dsh-launcher-windows-arm64.tar.gz -C dist/windows-arm64 .

echo ""
echo "=== Done! ==="
echo "Output:"
ls -lh dist/*.tar.gz
