cmake_minimum_required(VERSION 2.8.4)
project(PowerShell)

add_compile_options()

# set the output path for `powershell.exe`
set(EXECUTABLE_OUTPUT_PATH "${PROJECT_SOURCE_DIR}/../Microsoft.PowerShell.ConsoleHost")

# set these flags, so build does static linking for msvcr120.dll
# otherwise this dll need to be present on the system
set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} /MT")
set(CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG} /MTd")

include_directories(
    nativemsh/pwrshcommon)

link_directories(
    nativemsh/pwrshcommon)

add_definitions(-DUNICODE)
add_definitions(-D_UNICODE)

add_executable(powershell WIN32
    nativemsh/pwrshcommon/ConfigFileReader.cpp
    nativemsh/pwrshcommon/pwrshcommon.cpp
    nativemsh/pwrshcommon/WinSystemCallFacade.cpp

    nativemsh/pwrshexe/MainEntry.cpp)

# This subsystem definition is using old policy. TODO: figure out for release and the rest
set_target_properties(powershell PROPERTIES COMPILE_DEFINITIONS "_CONSOLE")
set_target_properties(powershell PROPERTIES LINK_FLAGS_DEBUG "/SUBSYSTEM:CONSOLE")
set_target_properties(powershell PROPERTIES LINK_FLAGS_RELWITHDEBINFO "/SUBSYSTEM:CONSOLE")
set_target_properties(powershell PROPERTIES LINK_FLAGS_RELEASE "/SUBSYSTEM:CONSOLE")
set_target_properties(powershell PROPERTIES LINK_FLAGS_MINSIZEREL "/SUBSYSTEM:CONSOLE")


target_link_libraries(powershell
    mscoree.lib
    MUILoad.lib
    kernel32.lib
    msxml6.lib
    legacy_stdio_definitions.lib)
