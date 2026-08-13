#!/bin/bash
# Package DSH Launcher for distribution
# Creates a ready-to-ship folder with all needed files

set -e

echo "=== DSH Launcher Packager ==="

# Build Windows version
echo "Building Windows version..."
cd DshLauncher.Windows
dotnet publish -c Release -r win-x64 --self-contained false -o ../dist/windows
cp start-dsh.vbs ../dist/windows/
cd ..

# Copy scripts
echo "Copying scripts..."
mkdir -p dist/scripts
cp scripts/start-dsh.bat dist/scripts/
cp scripts/start-dsh.sh dist/scripts/
chmod +x dist/scripts/start-dsh.sh

# Copy macOS/Linux source (users build themselves)
echo "Copying platform sources..."
cp -r DshLauncher.MacOS dist/
cp -r DshLauncher.Linux dist/

# Copy docs
cp README.md dist/
cp LICENSE dist/

echo ""
echo "=== Done! ==="
echo "Distribution files: dist/"
echo ""
echo "Windows: dist/windows/DshLauncher.Windows.exe"
echo "Scripts: dist/scripts/start-dsh.bat (Windows) or start-dsh.sh (macOS/Linux)"
