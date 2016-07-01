# 
# Builds pwrshplugin.dll, the WinRM plugin for PowerShell remote hosting.
#
add_library(pwrshplugin
    entrypoints.cpp
    pwrshclrhost.cpp
    pwrshpluginerrorcodes.mc
    pwrshpluginerrorcodes.rc
    pwrshplugin.def)

if (BUILD_ONECORE)
    # Libraries to use when creating this binary for Windows on OneCore-based SKUs
    set(PWRSHPLUGIN_WINDOWS_LIBS 
        onecore.lib)
    set(pwrshplugin_definitions ${common_pwrsh_definitions} CORECLR)
else (BUILD_ONECORE)
    # Libraries to use when creating this binary for Windows on full SKUs
    set(PWRSHPLUGIN_WINDOWS_LIBS
        ntdll.lib
        kernel32.lib
        advapi32.lib
        ole32.lib
        MUILoad.lib
        mscoree.lib
        oleaut32.lib
        uuid.lib
        wsmsvc.lib)
    set(pwrshplugin_definitions ${common_pwrsh_definitions})
endif (BUILD_ONECORE)

set_target_properties(pwrshplugin PROPERTIES COMPILE_DEFINITIONS "${pwrshplugin_definitions}")

target_link_libraries(pwrshplugin
    ${PWRSHPLUGIN_WINDOWS_LIBS})

#target_link_libraries(pwrshplugin LINK_PUBLIC pwrshcommon)
target_link_libraries(pwrshplugin pwrshcommon)
