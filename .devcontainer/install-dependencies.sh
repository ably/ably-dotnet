#!/bin/bash
set -e

echo "=========================================="
echo "Installing dependencies for Ably .NET development..."
echo "=========================================="
echo ""

# Update package lists
echo "📦 Updating package lists..."
sudo apt-get update -qq
echo "✓ Package lists updated"
echo ""

# Install prerequisites
echo "📦 Installing prerequisites (gnupg, ca-certificates, wget)..."
sudo apt-get install -y gnupg ca-certificates wget
echo "✓ Prerequisites installed"
echo ""

# Note: Installing Mono from Debian's official repositories
echo "ℹ️  Using Mono from Debian official repositories (bookworm)"
sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
echo "deb https://download.mono-project.com/repo/debian stable-buster main" | sudo tee /etc/apt/sources.list.d/mono-official-stable.list
echo ""

# Add Microsoft package repository for .NET runtimes
echo "🔑 Adding Microsoft package repository..."
wget -q https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
echo "✓ Microsoft repository added"
echo ""

# Update package lists with Microsoft repository
echo "📦 Updating package lists..."
sudo apt-get update -qq
echo "✓ Package lists updated"
echo ""

# Install Mono and NuGet
echo "🔧 Installing Mono and NuGet (this may take a few minutes)..."
sudo apt-get install -y mono-complete nuget
echo "✓ Mono and NuGet installed"
echo ""

# Install .NET 6.0 runtime
echo "🔧 Installing .NET 6.0 runtime..."
sudo apt-get install -y dotnet-runtime-6.0
echo "✓ .NET 6.0 runtime installed"
echo ""

# Install .NET 7.0 runtime
echo "🔧 Installing .NET 7.0 runtime..."
sudo apt-get install -y dotnet-runtime-7.0
echo "✓ .NET 7.0 runtime installed"
echo ""

echo "=========================================="
echo "✅ All dependencies installed successfully!"
echo "=========================================="
echo ""

echo "📋 Installed versions:"
echo ""
echo "Mono version:"
mono --version | head -n 1
echo ""
echo ".NET Runtimes:"
dotnet --list-runtimes
echo ""
echo ".NET SDKs:"
dotnet --list-sdks
echo ""
echo "NuGet version:"
nuget help | head -n 1
echo ""
echo "=========================================="
echo "🎉 Setup complete! You can now build the project."
echo "=========================================="
