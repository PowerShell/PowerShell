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

