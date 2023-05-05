#!/bin/bash
set -ex

# Get the macos universal pkg installer download url
# for the version you want to install from: https://download.mono-project.com/archive/
# and set it here as the value of this variable:
MONO_MACOS_PKG_DOWNLOAD_URL='https://download.mono-project.com/archive/6.12.0/macos-10-universal/MonoFramework-MDK-6.12.0.190.macos10.xamarin.universal.pkg'

# create a temp dir and cd into it
mkdir -p /tmp/mono-install
cd /tmp/mono-install

# debug: mono version before the install
mono --version

# download mono mac installer (pkg)
wget -q -O ./mono-installer.pkg "$MONO_MACOS_PKG_DOWNLOAD_URL"

# install it
sudo installer -pkg ./mono-installer.pkg -target /

# debug: mono version after install, just to confirm it did overwrite the original version
mono --version

# just for fun print this symlink too, which should point to the version we just installed
ls -alh /Library/Frameworks/Mono.framework/Versions/Current
