#!/bin/bash
# Build script for macOS
# Run on macOS to compile the launcher

set -e

echo "=== Building DSH Launcher for macOS ==="

# Check Swift
if ! command -v swiftc &> /dev/null; then
    echo "Error: Swift compiler not found. Install Xcode Command Line Tools:"
    echo "  xcode-select --install"
    exit 1
fi

# Build
cd DshLauncher.MacOS
swiftc main.swift -o dsh-launcher -framework Cocoa -framework WebKit -O

# Package
mkdir -p ../dist
cp dsh-launcher ../dist/
cp start-dsh.sh ../dist/
cp ../assets/icon.jpg ../dist/icon.png 2>/dev/null || true
chmod +x ../dist/dsh-launcher ../dist/start-dsh.sh

cd ..
tar -czf dist/dsh-launcher-macos.tar.gz -C dist .

echo ""
echo "=== Done! ==="
echo "Output: dist/dsh-launcher-macos.tar.gz"
echo "Run: ./dist/dsh-launcher"
