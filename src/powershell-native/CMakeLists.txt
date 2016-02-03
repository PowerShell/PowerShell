cmake_minimum_required(VERSION 2.8.4)
project(PowerShell)

add_compile_options()

include_directories(
    ../../src/monad/monad/nttargets/assemblies/nativemsh/pwrshcommon)

link_directories(
    ../../src/monad/monad/nttargets/assemblies/nativemsh/pwrshcommon)

add_definitions(-DUNICODE)
add_definitions(-D_UNICODE)

add_executable(powershell WIN32
    ../../src/monad/monad/nttargets/assemblies/nativemsh/pwrshcommon/ConfigFileReader.cpp
    ../../src/monad/monad/nttargets/assemblies/nativemsh/pwrshcommon/pwrshcommon.cpp
    ../../src/monad/monad/nttargets/assemblies/nativemsh/pwrshcommon/WinSystemCallFacade.cpp

    ../../src/monad/monad/nttargets/assemblies/nativemsh/pwrshexe/MainEntry.cpp)

# This subsystem definition is using old policy. TODO: figure out for release and the rest
set_target_properties(powershell PROPERTIES LINK_FLAGS_DEBUG "/SUBSYSTEM:CONSOLE")
set_target_properties(powershell PROPERTIES COMPILE_DEFINITIONS_DEBUG "_CONSOLE")
set_target_properties(powershell PROPERTIES LINK_FLAGS_RELWITHDEBINFO "/SUBSYSTEM:CONSOLE")
set_target_properties(powershell PROPERTIES COMPILE_DEFINITIONS_RELWITHDEBINFO "_CONSOLE")
set_target_properties(powershell PROPERTIES LINK_FLAGS_RELEASE "/SUBSYSTEM:WINDOWS")
set_target_properties(powershell PROPERTIES LINK_FLAGS_MINSIZEREL "/SUBSYSTEM:WINDOWS")


target_link_libraries(powershell
    mscoree.lib
    MUILoad.lib
    kernel32.lib
    msxml6.lib)
