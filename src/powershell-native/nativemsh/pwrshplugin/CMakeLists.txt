# 
# Builds pwrshplugin.dll, the WinRM plugin for PowerShell remote hosting.
#
add_library(pwrshplugin SHARED
    entrypoints.cpp
    pwrshclrhost.cpp
    pwrshpluginerrorcodes.mc
    pwrshpluginerrorcodes.rc
    pwrshplugin.def)

if (BUILD_ONECORE)
    message("*")
    message("* Setting up BUILD_ONECORE version of pwrshplugin.dll")
    message("*")
    # Libraries to use when creating this binary for Windows on OneCore-based SKUs
    set(PWRSHPLUGIN_WINDOWS_LIBS 
###        ${STATIC_MT_CRT_LIB}
###        ${STATIC_MT_VCRT_LIB}
###        ${STATIC_UCRT_LIB}
        onecore.lib
        vcruntime.lib
        ucrtd.lib
        msvcrt.lib
        wsmsvc.lib)
    #set(pwrshplugin_definitions ${common_pwrsh_definitions} CORECLR)
    set(pwrshplugin_definitions CORECLR)
    set_target_properties(pwrshplugin PROPERTIES COMPILE_DEFINITIONS "${pwrshplugin_definitions}")
else ()
    message("*")
    message("* Setting up NOT BUILD_ONECORE version of pwrshplugin.dll")
    message("*")
    # Libraries to use when creating this binary for Windows on full SKUs
    set(PWRSHPLUGIN_WINDOWS_LIBS
#        ntdll.lib
#        kernel32.lib
#        advapi32.lib
#        ole32.lib
        MUILoad.lib
        mscoree.lib
#        oleaut32.lib
#        uuid.lib
        wsmsvc.lib
        # CoreCLR lib
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_VCRT_LIB}
        ${STATIC_UCRT_LIB}
        legacy_stdio_definitions.lib # Resolves: LNK2019: unresolved external symbol _vsnwprintf
        )
    set(pwrshplugin_definitions ${common_pwrsh_definitions})
endif (BUILD_ONECORE)

#set_target_properties(pwrshplugin PROPERTIES COMPILE_DEFINITIONS "${pwrshplugin_definitions}")
#set_target_properties(pwrshplugin PROPERTIES COMPILE_DEFINITIONS "_DLL")

target_link_libraries(pwrshplugin
    ${PWRSHPLUGIN_WINDOWS_LIBS})

#target_link_libraries(pwrshplugin LINK_PUBLIC pwrshcommon)
target_link_libraries(pwrshplugin pwrshcommon)
