#!/bin/bash
# Build script for Linux
# Run on Linux to package the launcher

set -e

echo "=== Building DSH Launcher for Linux ==="

# Check Python
if ! command -v python3 &> /dev/null; then
    echo "Error: Python 3 not found"
    exit 1
fi

# Check GTK4
if ! python3 -c "import gi; gi.require_version('Gtk', '4.0'); from gi.repository import Gtk" 2>/dev/null; then
    echo "Installing GTK4 and WebKit dependencies..."
    sudo apt-get update
    sudo apt-get install -y python3-gi python3-gi-cairo gir1.2-webkit-6.0 gir1.2-gtk-4.0
fi

# Package
mkdir -p dist
cp DshLauncher.Linux/launcher.py dist/
cp DshLauncher.Linux/start-dsh.sh dist/
cp assets/icon.jpg dist/icon.jpg 2>/dev/null || true
chmod +x dist/launcher.py dist/start-dsh.sh

tar -czf dist/dsh-launcher-linux.tar.gz -C dist .

echo ""
echo "=== Done! ==="
echo "Output: dist/dsh-launcher-linux.tar.gz"
echo "Run: ./dist/launcher.py"
