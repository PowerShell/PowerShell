#
# Builds PowerShell.exe, the native host for PowerShell.
#

if (BUILD_ONECORE)
    # Settings to use when creating PowerShell.exe for Windows on OneCore-based SKUs
    set(PWRSHEXE_WINDOWS_SOURCES 
        CssMainEntry.cpp
        )
    set(PWRSHEXE_WINDOWS_LIBS
###        ${STATIC_MT_CRT_LIB}
###        ${STATIC_MT_VCRT_LIB}
###        ${STATIC_UCRT_LIB}
        onecore.lib
        vcruntime.lib
        ucrtd.lib
        msvcrt.lib
        msvcrt.lib
        msvcprtd.lib
        user32.lib
        atls.lib
        )
     set(powershell_definitions 
         _CONSOLE
         CORECLR
         )
else ()
    # Settings to use when creating PowerShell.exe for Windows on full SKUs or downlevel platforms
    set(PWRSHEXE_WINDOWS_SOURCES 
        MainEntry.cpp
        )
    # Most libs are automatically added by VS
    set(PWRSHEXE_WINDOWS_LIBS
        # CoreCLR libs
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_VCRT_LIB}
        ${STATIC_UCRT_LIB}
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
        )
endif (BUILD_ONECORE)

add_executable(powershell
    ${PWRSHEXE_WINDOWS_SOURCES})

set_target_properties(powershell PROPERTIES COMPILE_DEFINITIONS "${powershell_definitions}")

target_link_libraries(powershell ${PWRSHEXE_WINDOWS_LIBS})
target_link_libraries(powershell pwrshcommon)
