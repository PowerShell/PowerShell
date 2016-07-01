#
# Builds PowerShell.exe, the native host for PowerShell.
#
if (BUILD_ONECORE)
    # Settings to use when creating PowerShell.exe for Windows on OneCore-based SKUs
    set(PWRSHEXE_WINDOWS_SOURCES 
        # TODO: Fix this once the file is migrated from SD
        #        CssMainEntry.cpp)
        MainEntry.cpp)
    set(PWRSHEXE_WINDOWS_LIBS
        onecore.lib)
    set(powershell_definitions 
        _CONSOLE
        CORECLR
        ${common_pwrsh_definitions})
else (BUILD_ONECORE)
    # Settings to use when creating PowerShell.exe for Windows on full SKUs or downlevel platforms
    set(PWRSHEXE_WINDOWS_SOURCES 
        MainEntry.cpp)
    # Most libs are automatically added by VS
    set(PWRSHEXE_WINDOWS_LIBS
        #        ole32.lib
        #        oleaut32.lib
        #        uuid.lib
        #        user32.lib
        MUILoad.lib
        msxml6.lib
        mscoree.lib
        )
    set(powershell_definitions 
        _CONSOLE
        ${common_pwrsh_definitions})
endif (BUILD_ONECORE)

add_executable(powershell
    ${PWRSHEXE_WINDOWS_SOURCES})

set_target_properties(powershell PROPERTIES COMPILE_DEFINITIONS "${powershell_definitions}")
set_target_properties(powershell PROPERTIES LINK_FLAGS_DEBUG "/SUBSYSTEM:CONSOLE")
set_target_properties(powershell PROPERTIES LINK_FLAGS_RELWITHDEBINFO "/SUBSYSTEM:CONSOLE")
set_target_properties(powershell PROPERTIES LINK_FLAGS_RELEASE "/SUBSYSTEM:CONSOLE")
set_target_properties(powershell PROPERTIES LINK_FLAGS_MINSIZEREL "/SUBSYSTEM:CONSOLE")

target_link_libraries(powershell
    ${PWRSHEXE_WINDOWS_LIBS})

#target_link_libraries(powershell LINK_PUBLIC pwrshcommon)
target_link_libraries(powershell pwrshcommon)
