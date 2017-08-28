#
# CMake Instructions for producing pwrshcommon.lib for consumption by powershell.exe and
# pwrshplugin.dll. It is a library that is statically compiled into each binary.
#
add_library(pwrshcommon
    pwrshcommon.cpp
    WinSystemCallFacade.cpp
    ConfigFileReader.cpp
    )

target_include_directories(pwrshcommon PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})

#
# VS 2017 corrupts the INCLUDE environment variable when processing the csproj
# file generated for this template. The netfxsdk path is shortened from
# C:\Program Files (x86)\Windows Kits\NETFXSDK\4.6.1\Include\um
# to
# Include\um
# This prevents msbuild from locating corerror.h and throws a resolution error
# during compilation. We work around the problem by force-including the
# contents of the environment variable before it gets corrupted.
#
target_include_directories(pwrshcommon PUBLIC $ENV{INCLUDE})

if (BUILD_ONECORE)
    # Libraries to use when creating this binary for Windows on OneCore-based SKUs
    set(PWRSHCOMMON_WINDOWS_LIBS 
        )
    set_target_properties(pwrshcommon PROPERTIES COMPILE_DEFINITIONS "CORECLR")
else () # NOT BUILD_ONECORE
    # Libraries to use when creating this binary for Windows on full SKUs
    # Note: The appropriate libs get added automatically by VS
    set(PWRSHCOMMON_WINDOWS_LIBS
        )
endif (BUILD_ONECORE)

target_link_libraries(pwrshcommon
    ${PWRSHCOMMON_WINDOWS_LIBS})

