#
# Builds PowerShell.exe, the native host for PowerShell.
#

if (BUILD_ONECORE)
    # Settings to use when creating PowerShell.exe for Windows on OneCore-based SKUs
    set(PWRSHEXE_WINDOWS_SOURCES 
        CssMainEntry.cpp
        )
    set(PWRSHEXE_WINDOWS_LIBS
        onecore.lib
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_VCRT_LIB}
        )
    set(powershell_definitions
        _CONSOLE
        CORECLR
        )

    #
    # Configure lib directories so that CI picks up onecore.lib
    #
    SET (WindowsSdkDir $ENV{WindowsSdkDir})
    SET (WindowsSDKVersion $ENV{WindowsSDKVersion})
    SET (WindowsSDKLibBase "${WindowsSdkDir}/Lib/${WindowsSDKVersion}")
    SET (OneCoreLibBase "$ENV{VCInstallDir}lib/onecore/amd64")

    SET (LibraryPath)
    list (APPEND LibraryPath "${OneCoreLibBase}")
    list (APPEND LibraryPath "${WindowsSDKLibBase}ucrt/${WindowsSDKPlatform}")
    list (APPEND LibraryPath "${WindowsSDKLibBase}um/${WindowsSDKPlatform}" )
    link_directories(${LibraryPath})

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
