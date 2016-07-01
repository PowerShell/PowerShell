cmake_minimum_required(VERSION 2.8.4)
project(PowerShell)

add_compile_options()

# set the output path for `powershell.exe`
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${PROJECT_SOURCE_DIR}/../Microsoft.PowerShell.ConsoleHost")
foreach(OUTPUTCONFIG ${CMAKE_CONFIGURATION_TYPES})
    string(TOUPPER ${OUTPUTCONFIG} OUTPUTCONFIG)
    set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_${OUTPUTCONFIG} ${CMAKE_RUNTIME_OUTPUT_DIRECTORY})
endforeach( OUTPUTCONFIG CMAKE_CONFIGURATION_TYPES )

#
# TODO: Will this interfere with OneCore compilations?
#
# set these flags, so build does static linking for msvcr120.dll
# otherwise this dll need to be present on the system
set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} /MT")
set(CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG} /MTd")

# Compiler flags that are universal to all powershell native binaries
SET(common_pwrsh_definitions 
    UNICODE
    _UNICODE)

# Default of BUILD_ONECORE should be ON once it is supported 
option(BUILD_ONECORE "Compile the OneCore version of the binaries" OFF)
option(BUILD_DOWNLEVEL "Compile the downlevel version of the binaries" OFF)

# Build the common library that powershell.exe and pwrshplugin.dll depend on
add_subdirectory(nativemsh/pwrshcommon)

# Build pwrshplugin.dll
add_subdirectory(nativemsh/pwrshplugin)

# Build powershell.exe
add_subdirectory(nativemsh/pwrshexe)

