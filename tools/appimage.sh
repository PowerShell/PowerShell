#!/bin/bash

# The MIT License (MIT)
# Copyright (c) 2016 Simon Peter
# 
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:
# 
# The above copyright notice and this permission notice shall be included in all
# copies or substantial portions of the Software.
# 
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
# SOFTWARE.

# Generate AppImage, http://appimage.org
#
# The resulting PowerShell AppImage is known to run on
# CentOS-7.0-1406-x86_64-GnomeLive.iso
# CentOS-7-x86_64-LiveGNOME-1511.iso
# Chromixium-1.5-amd64.iso
# Fedora-Live-Workstation-x86_64-22-3.iso
# Fedora-Live-Workstation-x86_64-23-10.iso
# SL-72-x86_64-2016-02-03-LiveDVDgnome.iso
# debian-live-8.0.0-amd64-xfce-desktop+nonfree.iso
# debian-live-8.4.0-amd64-gnome-desktop.iso
# elementary_OS_0.3_freya_amd64.iso
# kali-linux-2.0-amd64.iso
# kali-linux-2016.1-amd64.iso
# kubuntu-14.04.4-desktop-amd64.iso
# kubuntu-15.04-desktop-amd64.iso
# kubuntu-16.04-desktop-amd64.iso
# linuxmint-17.3-cinnamon-64bit.iso
# neon-devedition-gitunstable-20160814-0806-amd64.iso
# netrunner-17-64bit.iso
# ubuntu-14.04.1-desktop-amd64.iso
# ubuntu-16.04-desktop-amd64.iso
# ubuntu-gnome-16.04-desktop-amd64.iso
# ubuntu-mate-16.04-desktop-amd64.iso
# xubuntu-16.04-desktop-amd64.iso

APP=PowerShell
LOWERAPP=${APP,,}

mkdir -p ./$APP/$APP.AppDir/usr/lib

cd ./$APP/

wget -q https://github.com/probonopd/AppImages/raw/master/functions.sh -O ./functions.sh
. ./functions.sh

# We get this app and almost all its dependencies via apt-get
# but not using the host system's information about what is
# installed on the system but our own assumptions instead

mkdir -p ./tmp/archives/
mkdir -p ./tmp/lists/partial
touch tmp/pkgcache.bin tmp/srcpkgcache.bin

generate_status

echo "deb http://archive.ubuntu.com/ubuntu/ trusty main universe
" > sources.list

OPTIONS="-o Debug::NoLocking=1
-o APT::Cache-Limit=125829120
-o Dir::Etc::sourcelist=./sources.list
-o Dir::State=./tmp
-o Dir::Cache=./tmp
-o Dir::State::status=./status
-o Dir::Etc::sourceparts=-
-o APT::Get::List-Cleanup=0
-o APT::Get::AllowUnauthenticated=1
-o Debug::pkgProblemResolver=true
-o Debug::pkgDepCache::AutoInstall=true
-o APT::Install-Recommends=0
-o APT::Install-Suggests=0
"

cp ../powershell_*_amd64.deb .

# Add local repository so that we can install deb files
# that were downloaded outside of a repository
dpkg-scanpackages . /dev/null | gzip -9c > Packages.gz
echo "deb file:$(readlink -e $PWD) ./" >> sources.list

apt-get $OPTIONS update

URLS=$(apt-get $OPTIONS -y install --print-uris $LOWERAPP | cut -d "'" -f 2 | grep -e "^http")

wget -c $URLS

cd ./$APP.AppDir/

find ../*.deb -exec dpkg -x {} . \; || true

rm usr/bin/powershell
mv opt/microsoft/powershell/*/* usr/bin/

cat > $LOWERAPP.desktop <<\EOF
[Desktop Entry]
Name=PowerShell
Comment=Microsoft PowerShell
Exec=powershell
Keywords=shell;prompt;command;commandline;cmd;
Icon=powershell
Type=Application
Categories=System;TerminalEmulator;
StartupNotify=true
Terminal=true
EOF

wget -c "https://github.com/PowerShell/PowerShell/raw/master/assets/Powershell_256.png" -O $LOWERAPP.png

cat > ./AppRun <<\EOF
#!/bin/sh
HERE=$(dirname $(readlink -f "${0}"))
export PATH="${HERE}/usr/bin/":$PATH
export LD_LIBRARY_PATH="${HERE}/usr/lib/":$LD_LIBRARY_PATH
exec "${HERE}/usr/bin/powershell.wrapper" "$@"
EOF
chmod a+x ./AppRun

# Copy in the indirect dependencies
# copy_deps # not needed here due to the way we use apt to download everything

move_lib
mv ./usr/lib/x86_64-linux-gnu/* ./usr/lib/ # AppRun sets Qt env here

mv ./usr/lib/pulseaudio/*.so usr/lib/ || true

mv usr/local/share/man usr/share/

delete_blacklisted
rm -rf ./etc/ ./home/ ./lib/ || true
rm -r opt/ usr/lib/x86_64-linux-gnu/ usr/lib64 usr/share/

# patch_usr
# Patching only the executable files seems not to be enough for some apps
# find usr/ -type f -exec sed -i -e "s|/usr|././|g" {} \;

VER1=$(find ../*.deb -name $LOWERAPP"_*" | head -n 1 | cut -d "~" -f 1 | cut -d "_" -f 2 | cut -d "-" -f 1-2 | sed -e 's|1%3a||g' | sed -e 's|-|.|g' )
# GLIBC_NEEDED=$(glibc_needed)
VERSION=$VER1
echo $VERSION

get_desktopintegration $LOWERAPP
sed -i -e 's|^echo|# echo|g' usr/bin/$LOWERAPP.wrapper # Make less verbose

# Go out of AppImage
cd ..

ARCH="x86_64"
generate_appimage

cp ../out/*AppImage ..
