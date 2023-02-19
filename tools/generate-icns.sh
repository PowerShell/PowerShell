#!/usr/bin/env bash

lowercase(){
    echo "$1" | sed "y/ABCDEFGHIJKLMNOPQRSTUVWXYZ/abcdefghijklmnopqrstuvwxyz/"
}

display_usage() {
    echo
    echo "  Usage: ./$(basename $0) svgfilename icnsfilename"
    echo
}

# Verify two arguments are passed.
if [ $# != 2 ];then
    display_usage
    exit 1
fi

# Verify input SVG exists.
if [ ! -f $1 ]; then
    echo "SVG file not found."
    display_usage
    exit 1
fi

# Verifying OS as iconutil is needed."
if [ $(lowercase $(uname)) != "darwin" ]; then
    echo "Unsupported platform. Please run under macOS."
    exit 1
fi

# Verify librsvg is installed.
if ! hash rsvg-convert 2>/dev/null; then
    echo "librsvg is not installed."
    exit 1
fi

# Create temporary folder for iconset.
guid=$(uuidgen)
if ! mkdir $guid.iconset >/dev/null 2>&1; then
    echo "Error creating temporary iconset folder."
    exit 1
fi

# Generate images for iconset.
rsvg-convert -f png -w 16   -h 16   -o  $guid.iconset/icon_16x16.png       $1
rsvg-convert -f png -w 32   -h 32   -o  $guid.iconset/icon_16x16@2x.png    $1
rsvg-convert -f png -w 32   -h 32   -o  $guid.iconset/icon_32x32.png       $1
rsvg-convert -f png -w 64   -h 64   -o  $guid.iconset/icon_32x32@2x.png    $1
rsvg-convert -f png -w 128  -h 128  -o  $guid.iconset/icon_128x128.png     $1
rsvg-convert -f png -w 256  -h 256  -o  $guid.iconset/icon_128x128@2x.png  $1
rsvg-convert -f png -w 256  -h 256  -o  $guid.iconset/icon_256x256.png     $1
rsvg-convert -f png -w 512  -h 512  -o  $guid.iconset/icon_256x256@2x.png  $1
rsvg-convert -f png -w 512  -h 512  -o  $guid.iconset/icon_512x512.png     $1
rsvg-convert -f png -w 1024 -h 1024 -o  $guid.iconset/icon_512x512@2x.png  $1

# Convert iconset to icns.
if ! iconutil -c icns $guid.iconset >/dev/null 2>&1; then
    echo "Error converting iconset to icns."
    exit 1
fi

# Remove temporary folder.
if ! rm -rf $guid.iconset >/dev/null 2>&1; then
    echo "Error removing temporary iconset folder."
    exit 1
fi

# Rename/Move icns file.
if ! mv $guid.icns $2 >/dev/null 2>&1; then
    echo "Error moving icns file."
    exit 1
fi
exit 0

