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
# Get the path to the Windows SDK build tools.
#
if (WindowsSDKPlatform STREQUAL "x64" OR WindowsSDKPlatform STREQUAL "X64" OR WindowsSDKPlatform STREQUAL "amd64" OR WindowsSDKPlatform STREQUAL "AMD64")
    # Normalize the platform name
    SET(WindowsSDKPlatform "x64")
elseif (WindowsSDKPlatform STREQUAL "x86" OR WindowsSDKPlatform STREQUAL "X86")
    # Normalize the platform name
    SET(WindowsSDKPlatform "x86")
else()
    message(FATAL_ERROR "Unsupported WindowsSDKPlatform ${WindowsSDKPlatform}")
endif ()

#
# set the output path for all binaries
#
if (BUILD_ONECORE)
    set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${PROJECT_SOURCE_DIR}/Bin/CoreClr")
else ()
    set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${PROJECT_SOURCE_DIR}/Bin/FullClr")
endif (BUILD_ONECORE)
foreach(OUTPUTCONFIG ${CMAKE_CONFIGURATION_TYPES})
    string(TOUPPER ${OUTPUTCONFIG} OUTPUTCONFIG)
    set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_${OUTPUTCONFIG} ${CMAKE_RUNTIME_OUTPUT_DIRECTORY})
    message("  Setting output directory for ${OUTPUTCONFIG} to ${CMAKE_RUNTIME_OUTPUT_DIRECTORY}")
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

# ***
# *** Original flags: BEGIN
# ***
#
# TODO: Will this interfere with OneCore compilations?
#
# set these flags, so build does static linking for msvcr120.dll
# otherwise this dll need to be present on the system
#set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} /MT")
#set(CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG} /MTd")

# Compiler flags that are universal to all powershell native binaries
#SET(common_pwrsh_definitions 
#    UNICODE
#    _UNICODE)

# ***
# *** Original flags: END
# ***

#include(dsc_defs.cmake)
if (BUILD_ONECORE)
    include(nano_defs.cmake)
else ()
    include(coreclr_defs.cmake)
endif (BUILD_ONECORE)

# Default of BUILD_ONECORE should be ON once it is supported 
option(BUILD_ONECORE "Compile the OneCore version of the binaries" OFF)
option(BUILD_DOWNLEVEL "Compile the downlevel version of the binaries" OFF)

# Build the common library that powershell.exe and pwrshplugin.dll depend on
add_subdirectory(nativemsh/pwrshcommon)

# Build pwrshplugin.dll
add_subdirectory(nativemsh/pwrshplugin)

# Build powershell.exe
#if (NOT BUILD_ONECORE)
    # Conditionally build powershell.exe until CssMainEntry.cpp appears.
    add_subdirectory(nativemsh/pwrshexe)
#endif ()
