#!/bin/bash

# This code is based on an example recipe from the AppImage project,
# https://github.com/probonopd/AppImages/blob/e05cbebc62c86f8c602d74d9050bbfbf10df1c69/recipes/powershell/Recipe
# Copyright (c) 2016 Simon Peter
# The license of this code and of https://github.com/probonopd/AppImages/raw/e05cbebc62c86f8c602d74d9050bbfbf10df1c69/functions.sh
# is the MIT License, see https://github.com/probonopd/AppImages/blob/e05cbebc62c86f8c602d74d9050bbfbf10df1c69/LICENSE
#
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

APP=powershell

# Generate status file for use by apt-get; assuming that the recipe uses no newer
# ingredients that would require more recent dependencies than what we assume to
# be part of the base system
generate_status()
{
  mkdir -p ./tmp/archives/
  mkdir -p ./tmp/lists/partial
  touch tmp/pkgcache.bin tmp/srcpkgcache.bin
  rm status 2>/dev/null || true
  for PACKAGE in "apt apt-transport-https dbus debconf dictionaries-common dpkg fontconfig fontconfig-config gksu glib-networking gstreamer1.0-plugins-base gstreamer1.0-plugins-good gstreamer1.0-plugins-ugly gstreamer1.0-pulseaudio gtk2-engines-pixbuf gvfs-backends kde-runtime libasound2 libatk1.0-0 libc6 libc6-dev libcairo2 libcups2 libdbus-1-3 libdrm2 libegl1-mesa libfontconfig1 libgbm1 libgcc1 libgdk-pixbuf2.0-0 libgl1 libgl1-mesa libgl1-mesa-dri libgl1-mesa-glx libglib2.0-0 libglu1-mesa libgpg-error0 libgtk2.0-0 libgtk-3-0 libnss3 libpango-1.0-0 libpango1.0-0 libpangocairo-1.0-0 libpangoft2-1.0-0 libstdc++6 libtasn1-6 libwayland-egl1-mesa libxcb1 lsb-base mime-support passwd udev uuid-runtime" ; do
    printf "Package: $PACKAGE\nStatus: install ok installed\nArchitecture: all\nVersion: 9:999.999.999\n\n" >> status
  done
}

# Delete blacklisted files
delete_blacklisted()
{
  BLACKLISTED_FILES="libc.so.6 libdl.so.2 libstdc++.so.6 libm.so.6 libpthread.so.0 libresolv.so.2 libGL.so.1 libdrm.so.2 libxcb.so.1 libX11.so.6 libgio-2.0.so.0 libgdk-x11-2.0.so.0 libgtk-x11-2.0.so.0 libasound.so.2 libgdk_pixbuf-2.0.so.0 libfontconfig.so.1 libselinux.so.1 libcom_err.so.2 libcrypt.so.1 libexpat.so.1 libgcc_s.so.1 libglib-2.0.so.0 libgpg-error.so.0 libgssapi_krb5.so.2 libhcrypto.so.4 libhx509.so.5 libICE.so.6 libidn.so.11 libk5crypto.so.3 libkeyutils.so.1 libkrb5.so.26 libkrb5.so.3 libkrb5support.so.0 libm.so.6 libp11-kit.so.0 libpthread.so.0 libresolv.so.2 libroken.so.18 librt.so.1 libSM.so.6 libusb-1.0.so.0 libuuid.so.1 libwind.so.0 libz.so.1 libGL.so.1 libdrm.so.2 libgobject-2.0.so.0 libgpg-error.so.0 libxcb.so.1"
  echo $BLACKLISTED_FILES
  for FILE in $BLACKLISTED_FILES ; do
    FOUND=$(find . -type f -name "${FILE}" 2>/dev/null)
    if [ ! -z "$FOUND" ] ; then
      echo "Deleting blacklisted ${FOUND}"
      rm -f "${FOUND}"
    fi
  done

  # Do not bundle developer stuff
  rm -rf usr/include || true
  rm -rf usr/lib/cmake || true
  rm -rf usr/lib/pkgconfig || true
  find . -name '*.la' | xargs -i rm {}
}

# Add desktop integration
# Usage: get_desktopintegration name_of_desktop_file_and_exectuable
get_desktopintegration()
{
  REALBIN=$(grep -o "^Exec=.*" *.desktop | sed -e 's|Exec=||g' | cut -d " " -f 1 | head -n 1)
  cat > ./usr/bin/$REALBIN.wrapper <<\EOxxF
#!/bin/bash

# The purpose of this script is to provide lightweight desktop integration
# into the host system without special help from the host system.
# If you want to use it, then place this in usr/bin/$APPNAME.wrapper
# and set it as the Exec= line of the .desktop file in the AppImage.
#
# For example, to install the appropriate icons for Scribus,
# put them into the AppDir at the following locations:
#
# ./usr/share/icons/default/128x128/apps/scribus.png
# ./usr/share/icons/default/128x128/mimetypes/application-vnd.scribus.png
#
# Note that the filename application-vnd.scribus.png is derived from
# and must be match MimeType=application/vnd.scribus; in scribus.desktop
# (with "/" characters replaced by "-").
#
# Then, change Exec=scribus to Exec=scribus.wrapper and place the script
# below in usr/bin/scribus.wrapper and make it executable.
# When you run AppRun, then AppRun runs the wrapper script below
# which in turn will run the main application.
#
# TODO:
# Handle multiple versions of the same AppImage?
# Handle removed AppImages? Currently we are just setting TryExec=
# See http://specifications.freedesktop.org/thumbnail-spec/thumbnail-spec-latest.html#DELETE
# Possibly move this to the C runtime that is part of every AppImage?

# Exit on errors
set -e

# Be verbose if $DEBUG=1 is set
if [ ! -z "$DEBUG" ] ; then
  env
  set -x
fi

THIS="$0"
args=("$@") # http://stackoverflow.com/questions/3190818/
NUMBER_OF_ARGS="$#"

# Please do not change $VENDORPREFIX as it will allow for desktop files
# belonging to AppImages to be recognized by future AppImageKit components
# such as desktop integration daemons
VENDORPREFIX=appimagekit

find-up () {
  path="$(dirname "$(readlink -f "${THIS}")")"
  while [[ "$path" != "" && ! -e "$path/$1" ]]; do
    path=${path%/*}
  done
  # echo "$path"
}

if [ -z $APPDIR ] ; then
  # Find the AppDir. It is the directory that contains AppRun.
  # This assumes that this script resides inside the AppDir or a subdirectory.
  # If this script is run inside an AppImage, then the AppImage runtime
  # likely has already set $APPDIR
  APPDIR=$(find-up "AppRun")
fi

FILENAME="$(readlink -f "${THIS}")"
DIRNAME=$(dirname $FILENAME)

DESKTOPFILE=$(find "$APPDIR" -maxdepth 1 -name "*.desktop" | head -n 1)
DESKTOPFILE_NAME=$(basename "${DESKTOPFILE}")

APP_FULL=$(sed -n -e 's/^Name=//p' "${DESKTOPFILE}" | head -n 1)
APP=$(echo "$APP_FULL" | tr -c -d '[:alnum:]')
if [ -z "$APP" ] || [ -z "$APP_FULL" ] ; then
  APP=$(echo "$DESKTOPFILE_NAME" | sed -e 's/.desktop//g')
  APP_FULL="$APP"
fi

RETURN="yes"

if [[ "$FILENAME" != *.wrapper ]] ; then
  echo "${THIS} is not named correctly. It should be named \$Exec.wrapper"
  exit 0
fi

BIN=$(echo "$FILENAME" | sed -e 's|.wrapper||g')
if [[ ! -f "$BIN" ]] ; then
  echo "$BIN not found"
  exit 0
fi

trap atexit EXIT

# Note that the following handles 0, 1 or more arguments (file paths)
# which can include blanks but uses a bashism; can the same be achieved
# in POSIX-shell? (FIXME)
# http://stackoverflow.com/questions/3190818
atexit()
{
  if [ -z "$SKIP" ] ; then
    if [ $NUMBER_OF_ARGS -eq 0 ] ; then
      exec "${BIN}"
    else
      exec "${BIN}" "${args[@]}"
    fi
  fi
}

error()
{
  if [ -x /usr/bin/zenity ] ; then
    LD_LIBRARY_PATH="" zenity --error --text "${1}" 2>/dev/null
  elif [ -x /usr/bin/kdialog ] ; then
    LD_LIBRARY_PATH="" kdialog --msgbox "${1}" 2>/dev/null
  elif [ -x /usr/bin/Xdialog ] ; then
    LD_LIBRARY_PATH="" Xdialog --msgbox "${1}" 2>/dev/null
  else
    echo "${1}"
  fi
  exit 1
}

yesno()
{
  TITLE=$1
  TEXT=$2
  if [ -x /usr/bin/zenity ] ; then
    LD_LIBRARY_PATH="" zenity --question --title="$TITLE" --text="$TEXT" 2>/dev/null && RETURN="yes" || RETURN="no"
  elif [ -x /usr/bin/kdialog ] ; then
    LD_LIBRARY_PATH="" kdialog --caption "" --title "$TITLE" -yesno "$TEXT" && RETURN="yes" || RETURN="no"
  elif [ -x /usr/bin/Xdialog ] ; then
    LD_LIBRARY_PATH="" Xdialog --title "$TITLE" --clear --yesno "$TEXT" 10 80 && RETURN="yes" || RETURN="no"
  else
    echo "zenity, kdialog, Xdialog missing. Skipping ${THIS}."
    exit 0
  fi
}

check_prevent()
{
  FILE=$1
  if [ -e "$FILE" ] ; then
    exit 0
  fi
}

check_dep()
{
  DEP=$1
  if [ -z $(which $DEP) ] ; then
    echo "$DEP is missing. Skipping ${THIS}."
    exit 0
  fi
}

# Determine where the desktop file should be installed
if [[ $EUID -ne 0 ]]; then
   DESTINATION_DIR_DESKTOP="$HOME/.local/share/applications"
   STAMP_DIR="$HOME/.local/share/$VENDORPREFIX"
   SYSTEM_WIDE=""
else
   # TODO: Check $XDG_DATA_DIRS
   DESTINATION_DIR_DESKTOP="/usr/local/share/applications"
   STAMP_DIR="/etc/$VENDORPREFIX"
   SYSTEM_WIDE="--mode system" # for xdg-mime and xdg-icon-resource
fi

# Remove desktop integration for this AppImage
if [ "x$1" = "x--remove-appimage-desktop-integration" ] ; then
  SKIP="yes"
  rm -f "$STAMP_DIR/${APP}_no_desktopintegration" "$DESTINATION_DIR_DESKTOP/$VENDORPREFIX-$DESKTOPFILE_NAME"
  check_dep xdg-desktop-menu
  xdg-desktop-menu forceupdate
  exit 0
fi

# Exit immediately if one of these files is present
# (e.g., because the desktop environment wants to handle desktop integration itself)
check_prevent "$HOME/.local/share/$VENDORPREFIX/no_desktopintegration"
check_prevent "/usr/share/$VENDORPREFIX/no_desktopintegration"
check_prevent "/etc/$VENDORPREFIX/no_desktopintegration"

# Exit immediately if appimaged is running
pidof appimaged >/dev/null 2>&1 && exit 0

# Exit immediately if $DESKTOPINTEGRATION is not empty
if [ ! -z "$DESKTOPINTEGRATION" ] ; then
  exit 0
fi

# Check whether dependencies are present in base system (we do not bundle these)
# http://cgit.freedesktop.org/xdg/desktop-file-utils/
check_dep desktop-file-validate
check_dep update-desktop-database
check_dep desktop-file-install
check_dep xdg-icon-resource
check_dep xdg-mime
check_dep xdg-desktop-menu

# Exit immediately if one of these files is present (disabled per app)
check_prevent "$HOME/.local/share/$VENDORPREFIX/${APP}_no_desktopintegration"
check_prevent "/usr/share/$VENDORPREFIX/${APP}_no_desktopintegration"
check_prevent "/etc/$VENDORPREFIX/${APP}_no_desktopintegration"

if [ ! -f "$DESKTOPFILE" ] ; then
  echo "Desktop file is missing. Please run ${THIS} from within an AppImage."
  exit 0
fi

if [ -z "$APPIMAGE" ] ; then
  APPIMAGE="$APPDIR/AppRun"
  # Not running from within an AppImage; hence using the AppRun for Exec=
fi

ICONFILE="$APPDIR/.DirIcon"

# $XDG_DATA_DIRS contains the default paths /usr/local/share:/usr/share
# desktop file has to be installed in an applications subdirectory
# of one of the $XDG_DATA_DIRS components
if [ -z "$XDG_DATA_DIRS" ] ; then
  echo "\$XDG_DATA_DIRS is missing. Please run ${THIS} from within an AppImage."
  exit 0
fi

# Check if the desktop file is already there
# and if so, whether it points to the same AppImage
if [ -e "$DESTINATION_DIR_DESKTOP/$VENDORPREFIX-$DESKTOPFILE_NAME" ] ; then
  # echo "$DESTINATION_DIR_DESKTOP/$VENDORPREFIX-$DESKTOPFILE_NAME already there"
  EXEC=$(grep "^Exec=" "$DESTINATION_DIR_DESKTOP/$VENDORPREFIX-$DESKTOPFILE_NAME" | head -n 1 | cut -d " " -f 1)
  # echo $EXEC
  if [ "Exec=\"$APPIMAGE\"" == "$EXEC" ] ; then
    exit 0
  fi
fi

# We ask the user only if we have found no reason to skip until here
if [ -z "$SKIP" ] ; then
  yesno "Install" "Would you like to integrate $APPIMAGE with your system?\n\nThis will add it to your applications menu and install icons.\nIf you don't do this you can still launch the application by double-clicking on the AppImage."
fi

if [ "$RETURN" = "no" ] ; then
  yesno "Disable question?" "Should this question be permanently disabled for $APP?\n\nTo re-enable this question you have to delete\n\"$STAMP_DIR/${APP}_no_desktopintegration\""
  if [ "$RETURN" = "yes" ] ; then
    mkdir -p "$STAMP_DIR"
    touch "$STAMP_DIR/${APP}_no_desktopintegration"
  fi
  exit 0
fi

# If the user has agreed, rewrite and install the desktop file, and the MIME information
if [ -z "$SKIP" ] ; then
  # desktop-file-install is supposed to install .desktop files to the user's
  # applications directory when run as a non-root user,
  # and to /usr/share/applications if run as root
  # but that does not really work for me...
  #
  # For Exec we must use quotes
  # For TryExec quotes is not supported, so, space must be replaced to \s
  # https://askubuntu.com/questions/175404/how-to-add-space-to-exec-path-in-a-thumbnailer-descrption/175567
  RESOURCE_NAME=$(echo "$VENDORPREFIX-$DESKTOPFILE_NAME" | sed -e 's/.desktop//g')
  desktop-file-install --rebuild-mime-info-cache \
    --vendor=$VENDORPREFIX --set-key=Exec --set-value="\"${APPIMAGE}\" %U" \
    --set-key=X-AppImage-Comment --set-value="Generated by ${THIS}" \
    --set-icon="$RESOURCE_NAME" --set-key=TryExec --set-value=${APPIMAGE// /\\s} "$DESKTOPFILE" \
    --dir "$DESTINATION_DIR_DESKTOP"
  chmod a+x "$DESTINATION_DIR_DESKTOP/"*
  # echo $RESOURCE_NAME

  # delete "Actions" entry and add an "Uninstall" action
  sed -i -e '/^Actions=/d' "$DESTINATION_DIR_DESKTOP/$VENDORPREFIX-$DESKTOPFILE_NAME"
  cat >> "$DESTINATION_DIR_DESKTOP/$VENDORPREFIX-$DESKTOPFILE_NAME" << EOF

Actions=Uninstall;

[Uninstall]
Name=Remove desktop integration for $APP_FULL
Exec="$APPIMAGE" --remove-appimage-desktop-integration

EOF

  # Install the icon files for the application; TODO: scalable
  ICONS=$(find "${APPDIR}/usr/share/icons/" -iwholename "*/apps/${APP}.png" 2>/dev/null || true)
  for ICON in $ICONS ; do
    ICON_SIZE=$(echo "${ICON}" | rev | cut -d "/" -f 3 | rev | cut -d "x" -f 1)
    xdg-icon-resource install --context apps --size ${ICON_SIZE} "${ICON}" "${RESOURCE_NAME}"
  done

  # Install mime type
  find "${APPDIR}/usr/share/mime/" -type f -name *xml -exec xdg-mime install $SYSTEM_WIDE --novendor {} \; 2>/dev/null || true

  # Install the icon files for the mime type; TODO: scalable
  ICONS=$(find "${APPDIR}/usr/share/icons/" -iwholename "*/mimetypes/*.png" 2>/dev/null || true)
  for ICON in $ICONS ; do
    ICON_SIZE=$(echo "${ICON}" | rev | cut -d "/" -f 3 | rev | cut -d "x" -f 1)
    xdg-icon-resource install --context mimetypes --size ${ICON_SIZE} "${ICON}" $(basename $ICON | sed -e 's/.png//g')
  done

  xdg-desktop-menu forceupdate
  gtk-update-icon-cache # for MIME
fi
EOxxF
  chmod a+x ./usr/bin/$REALBIN.wrapper

  sed -i -e "s|^Exec=$REALBIN|Exec=$REALBIN.wrapper|g" $1.desktop
}

mkdir -p ./$APP/$APP.AppDir/usr/lib

cd ./$APP/

# We get this app and almost all its dependencies via apt-get
# but not using the host system's information about what is
# installed on the system but our own assumptions instead

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

cp ../powershell*ubuntu.14.04_amd64.deb .

# Add local repository so that we can install deb files
# that were downloaded outside of a repository
dpkg-scanpackages . /dev/null | gzip -9c > Packages.gz
echo "deb file:$(readlink -e $PWD) ./" >> sources.list

apt-get $OPTIONS update

URLS=$(apt-get $OPTIONS -y install --print-uris $APP | cut -d "'" -f 2 | grep -e "^http")

wget -c $URLS

cd ./$APP.AppDir/

find ../*.deb -exec dpkg -x {} . \; || true

rm usr/bin/pwsh
mv opt/microsoft/powershell/*/* usr/bin/

cat > $APP.desktop <<\EOF
[Desktop Entry]
Name=powershell
Comment=Microsoft PowerShell
Exec=pwsh
Keywords=shell;prompt;command;commandline;cmd;
Icon=powershell
Type=Application
Categories=System;TerminalEmulator;
StartupNotify=true
Terminal=true
EOF

cp ../../assets/Powershell_256.png $APP.png
cp ../../assets/AppImageThirdPartyNotices.txt ThirdPartyNotices.txt

cat > ./AppRun <<\EOF
#!/bin/bash
HERE=$(dirname $(readlink -f "${0}"))
export PATH="${HERE}/usr/bin/":$PATH
export LD_LIBRARY_PATH="${HERE}/usr/lib/":$LD_LIBRARY_PATH
exec "${HERE}/usr/bin/pwsh.wrapper" "$@"
EOF
chmod a+x ./AppRun

mkdir -p ./usr/lib ./lib && find ./lib/ -exec cp -v --parents -rfL {} ./usr/ \; && rm -rf ./lib
mkdir -p ./usr/lib ./lib64 && find ./lib64/ -exec cp -v --parents -rfL {} ./usr/ \; && rm -rf ./lib64
mv ./usr/lib/x86_64-linux-gnu/* ./usr/lib/ # AppRun sets Qt env here
mv ./usr/lib/pulseaudio/*.so usr/lib/ || true
mv usr/local/share/man usr/share/ || true

delete_blacklisted
rm -rf ./etc/ ./home/ ./lib/ || true
rm -r opt/ usr/lib/x86_64-linux-gnu/ usr/lib64 usr/share/ || true

VERSION=$(find ../*.deb -name $APP"_*" | head -n 1 | cut -d "~" -f 1 | cut -d "_" -f 2 | cut -d "-" -f 1-2 | sed -e 's|1%3a||g')
echo $VERSION

get_desktopintegration $APP
sed -i -e 's|^echo|# echo|g' usr/bin/pwsh.wrapper # Make less verbose

# Go out of AppImage
cd ..

wget -c https://psgithub.blob.core.windows.net/files/appimagetool-x86_64.AppImage
chmod a+x appimagetool-x86_64.AppImage
./appimagetool-x86_64.AppImage ./powershell.AppDir
cp ./powershell*AppImage ..

cd ..
