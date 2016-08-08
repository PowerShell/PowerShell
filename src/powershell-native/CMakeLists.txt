cmake_minimum_required(VERSION 2.8.4)

project(PowerShellNative)

#
# Verify prerequisites
#
if (NOT $ENV{${WindowsSDKVersion}})
    message (FATAL_ERROR "WindowsSDKVersion environment variable not found")
endif ()

if (NOT $ENV{${Platform}})
    message(FATAL_ERROR "Platform environment variable not found")
else ()
    SET(WindowsSDKPlatform "$ENV{Platform}")
endif ()

#
# Normalize the platform name
#
# TODO: Only x64 and x86 are supported right now. This needs to be expanded to arm as well
#
if (WindowsSDKPlatform STREQUAL "x64" OR WindowsSDKPlatform STREQUAL "X64" OR WindowsSDKPlatform STREQUAL "amd64" OR WindowsSDKPlatform STREQUAL "AMD64")
    SET(WindowsSDKPlatform "x64")
elseif (WindowsSDKPlatform STREQUAL "x86" OR WindowsSDKPlatform STREQUAL "X86")
    SET(WindowsSDKPlatform "x86")
else()
    message(FATAL_ERROR "Unsupported WindowsSDKPlatform ${WindowsSDKPlatform}")
endif ()

#
# set the output path for all binaries
#
if (BUILD_ONECORE)
    set(PWRSH_NATIVE_OUTPUT_DIRECTORY "CoreClr")
else ()
    set(PWRSH_NATIVE_OUTPUT_DIRECTORY "FullClr")
endif (BUILD_ONECORE)

foreach(OUTPUTCONFIG ${CMAKE_CONFIGURATION_TYPES})
    string(TOUPPER ${OUTPUTCONFIG} OUTPUTCONFIG)
    set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_${OUTPUTCONFIG} "${PROJECT_SOURCE_DIR}/Bin/${OUTPUTCONFIG}/${PWRSH_NATIVE_OUTPUT_DIRECTORY}")
    #    message("  Setting output directory for ${CMAKE_RUNTIME_OUTPUT_DIRECTORY_${OUTPUTCONFIG}}")
endforeach( OUTPUTCONFIG CMAKE_CONFIGURATION_TYPES )

#
# Definitions for ease of reading
#
SET (WIN_VERSION_WIN10_RS1     0x0A000002)
SET (WIN_VERSION_WIN10_TH2     0x0A000001)
SET (WIN_VERSION_WIN10         0x0A00)
SET (WIN_VERSION_WINTHRESHOLD  0x0A00)
SET (WIN_VERSION_WINBLUE       0x0603)
SET (WIN_VERSION_WIN8          0x0602)
SET (WIN_VERSION_WIN7          0x0601)
SET (NTDDI_VERSION_WIN7        0x06010000)
SET (NTDDI_VERSION_WIN10       0x0A000002)
SET (WIN_VERSION_VISTA         0x0600)
SET (WIN_VERSION_LONGHORN      0x0600)
SET (WIN_VERSION_WS03          0x0502)
SET (WIN_VERSION_WINXP         0x0501)

#include(dsc_defs.cmake)
if (BUILD_ONECORE)
    include(coreclr_defs.cmake)
    #include(nano_defs.cmake)
else ()
    include(coreclr_defs.cmake)
endif (BUILD_ONECORE)

# Default of BUILD_ONECORE should be ON once it is supported 
option(BUILD_ONECORE "Compile the OneCore version of the binaries" ON)
option(BUILD_DOWNLEVEL "Compile the downlevel version of the binaries" OFF)

# Build the common library that powershell.exe and pwrshplugin.dll depend on
add_subdirectory(nativemsh/pwrshcommon)

# Build pwrshplugin.dll
add_subdirectory(nativemsh/pwrshplugin)

# Build powershell.exe
add_subdirectory(nativemsh/pwrshexe)
