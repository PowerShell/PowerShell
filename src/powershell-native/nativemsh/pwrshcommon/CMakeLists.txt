#
# CMake Instructions for producing pwrshcommon.lib for consumption by powershell.exe and
# pwrshplugin.dll. It is a library that is statically compiled into each binary.
#
add_library(pwrshcommon
    pwrshcommon.cpp
    WinSystemCallFacade.cpp)

target_include_directories(pwrshcommon PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})

if (BUILD_ONECORE)
    # Libraries to use when creating this binary for Windows on OneCore-based SKUs
    set(PWRSHCOMMON_WINDOWS_LIBS 
        onecore.lib)
    set(pwrshcommon_definitions ${common_pwrsh_definitions} CORECLR)
else () # !BUILD_ONECORE
    # Libraries to use when creating this binary for Windows on full SKUs
    # Note: The appropriate libs get added automatically by VS
    set(PWRSHCOMMON_WINDOWS_LIBS
        #        ole32.lib
        #        oleaut32.lib
        #        uuid.lib
        #        user32.lib
        #        MUILoad.lib
        #        msxml6.lib
        #        mscoree.lib)
        )
    set(pwrshcommon_definitions ${common_pwrsh_definitions})
    #    target_include_directories(pwrshcommon PUBLIC
    #        "%ProgramFiles(x86)%/Windows Kits/NETFXSDK/4.6/Include/um")
endif (BUILD_ONECORE)

set_target_properties(pwrshcommon PROPERTIES COMPILE_DEFINITIONS "${pwrshcommon_definitions}")

target_link_libraries(pwrshcommon
    ${PWRSHCOMMON_WINDOWS_LIBS})

