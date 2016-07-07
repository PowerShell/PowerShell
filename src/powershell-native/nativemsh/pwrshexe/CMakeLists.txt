#
# Builds PowerShell.exe, the native host for PowerShell.
#

# Only build powershell.exe for "full" windows. Open PowerShell uses the managed executable.
if (BUILD_ONECORE)
    message("*")
    message("*********** Building PowerShell.exe for 'full' CLR instead of OneCore. Add CssMainEntry.cpp")
    message("*")
    #set(BUILD_ONECORE OFF)
endif ()

#if (BUILD_ONECORE)
    # Settings to use when creating PowerShell.exe for Windows on OneCore-based SKUs
#    set(PWRSHEXE_WINDOWS_SOURCES 
        # TODO: Fix this once the file is migrated from SD
        #        CssMainEntry.cpp)
#        MainEntry.cpp)
#    set(PWRSHEXE_WINDOWS_LIBS
#        onecore.lib)
#    set(powershell_definitions 
#        _CONSOLE
#        CORECLR
#        ${common_pwrsh_definitions})
#else (BUILD_ONECORE)
    # Settings to use when creating PowerShell.exe for Windows on full SKUs or downlevel platforms
    set(PWRSHEXE_WINDOWS_SOURCES 
        MainEntry.cpp)
    # Most libs are automatically added by VS
    set(PWRSHEXE_WINDOWS_LIBS
        # CoreCLR libs
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_VCRT_LIB}
        ${STATIC_UCRT_LIB}
        #
        #        ole32.lib
        #        oleaut32.lib
        #        uuid.lib
        #        user32.lib
        MUILoad.lib
        msxml6.lib
        mscoree.lib
        legacy_stdio_definitions.lib # Resolves: LNK2019: unresolved external symbol _vsnwprintf
        )
    set(powershell_definitions 
        _CONSOLE
        ${common_pwrsh_definitions})
#endif (BUILD_ONECORE)

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
