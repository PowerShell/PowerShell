cmake_minimum_required(VERSION 3.10.0)

project(PowerShellNative)

#
# Verify prerequisites
#
if (NOT $ENV{${WindowsSDKVersion}})
    message (FATAL_ERROR "WindowsSDKVersion environment variable not found")
endif ()

#
# Normalize the platform name
SET(BUILD_ARCH_ARM 0)
SET(BUILD_ARCH_ARM64 0)
SET(BUILD_ARCH_X86 0) 
SET(BUILD_ARCH_AMD64 0)

if (BUILD_TARGET_ARCH)
    SET(WindowsSDKPlatform ${BUILD_TARGET_ARCH})
    message(STATUS "Building for " ${BUILD_TARGET_ARCH})
else ()
    message(FATAL_ERROR "Target architecture value should be specified through BUILD_TARGET_ARCH. Supported values are x64, x86, arm, or arm64")
endif (BUILD_TARGET_ARCH)

if (WindowsSDKPlatform STREQUAL "x64" OR WindowsSDKPlatform STREQUAL "X64" OR WindowsSDKPlatform STREQUAL "amd64" OR WindowsSDKPlatform STREQUAL "AMD64")
    SET(WindowsSDKPlatform "x64")
    SET(BUILD_ARCH_AMD64 1)
elseif (WindowsSDKPlatform STREQUAL "x86" OR WindowsSDKPlatform STREQUAL "X86")
    SET(WindowsSDKPlatform "x86")
    SET(BUILD_ARCH_X86 1)
elseif (WindowsSDKPlatform STREQUAL "arm" OR WindowsSDKPlatform STREQUAL "ARM")
    SET(WindowsSDKPlatform "arm")
    SET(BUILD_ARCH_ARM 1)
elseif (WindowsSDKPlatform STREQUAL "arm64" OR WindowsSDKPlatform STREQUAL "ARM64")
    SET(WindowsSDKPlatform "arm64")
    SET(BUILD_ARCH_ARM64 1)
else()
    message(FATAL_ERROR "Unsupported WindowsSDKPlatform: " ${WindowsSDKPlatform})
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

include(coreclr_defs.cmake)

# Default of BUILD_ONECORE should be ON once it is supported 
option(BUILD_ONECORE "Compile the OneCore version of the binaries" ON)

# Build the common library that powershell.exe and pwrshplugin.dll depend on
add_subdirectory(nativemsh/pwrshcommon)

# Build pwrshplugin.dll
add_subdirectory(nativemsh/pwrshplugin)

# Build powershell.exe
add_subdirectory(nativemsh/pwrshexe)
