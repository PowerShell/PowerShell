cmake_minimum_required(VERSION 2.8.4)
project(PowerShell)

add_compile_options()

# set the output path for `powershell.exe`
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${PROJECT_SOURCE_DIR}/../Microsoft.PowerShell.ConsoleHost")
foreach(OUTPUTCONFIG ${CMAKE_CONFIGURATION_TYPES})
    string(TOUPPER ${OUTPUTCONFIG} OUTPUTCONFIG)
    set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_${OUTPUTCONFIG} ${CMAKE_RUNTIME_OUTPUT_DIRECTORY})
endforeach( OUTPUTCONFIG CMAKE_CONFIGURATION_TYPES )

# set these flags, so build does static linking for msvcr120.dll
# otherwise this dll need to be present on the system
set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} /MT")
set(CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG} /MTd")

include_directories(
    nativemsh/pwrshcommon
    nativemsh/pwrshplugin)

link_directories(
    nativemsh/pwrshcommon)

add_executable(powershell WIN32
    nativemsh/pwrshcommon/pwrshcommon.cpp
    nativemsh/pwrshcommon/WinSystemCallFacade.cpp
    nativemsh/pwrshexe/MainEntry.cpp)

add_library(pwrshplugin
    nativemsh/pwrshplugin/entrypoints.cpp
    nativemsh/pwrshplugin/pwrshclrhost.cpp
    #nativemsh/pwrshplugin/pwrshpluginerrorcodes.mc
    #nativemsh/pwrshplugin/pwrshpluginerrorcodes.rc
    nativemsh/pwrshplugin/pwrshplugin.def)


SET(powershell_definitions 
        _CONSOLE
        UNICODE
        _UNICODE)

set_target_properties(powershell PROPERTIES COMPILE_DEFINITIONS "${powershell_definitions}")
set_target_properties(powershell PROPERTIES LINK_FLAGS_DEBUG "/SUBSYSTEM:CONSOLE")
set_target_properties(powershell PROPERTIES LINK_FLAGS_RELWITHDEBINFO "/SUBSYSTEM:CONSOLE")
set_target_properties(powershell PROPERTIES LINK_FLAGS_RELEASE "/SUBSYSTEM:CONSOLE")
set_target_properties(powershell PROPERTIES LINK_FLAGS_MINSIZEREL "/SUBSYSTEM:CONSOLE")

SET(pwrshplugin_definitions 
        UNICODE
        _UNICODE)

set_target_properties(pwrshplugin PROPERTIES COMPILE_DEFINITIONS "${pwrshplugin_definitions}")

target_link_libraries(powershell
    mscoree.lib
    MUILoad.lib
    kernel32.lib
    msxml6.lib
    legacy_stdio_definitions.lib)

target_link_libraries(pwrshplugin)

