cmake_minimum_required(VERSION 3.5)
#
# Builds PowerShell.Core.Instrumentation.dll resource-only DLL containing ETW event resources
#

# The fully qualified path to the event manifest
SET(EVENTS_MANIFEST "${CMAKE_CURRENT_SOURCE_DIR}/PowerShell.Core.Instrumentation.man")

# User mode manifest resource-only dll
function(add_manifest_binary)

    add_definitions(-D_DLL=1)
    add_library(${ARGV})

    # NOTE: EVENTS_MANIFEST must be the fully qualified path to the manifest
    SET(MC_MANIFEST_FULLNAME ${EVENTS_MANIFEST})

    # get the ETW manifest's filename without the directory or extension
    get_filename_component(MC_MANIFEST_BASENAME ${EVENTS_MANIFEST} NAME_WE)

    SET(MC_COMMAND "mc.exe")
    SET(GeneratedManifestFiles)

    # The target directory for generated managed files
    SET (MC_GENERATED_DIR "${CMAKE_CURRENT_BINARY_DIR}")

    # include the generated directory in the include path
    include_directories("${MC_GENERATED_DIR}")

    SET (MC_GENERATED_FILES
        ${MC_GENERATED_DIR}/${MC_MANIFEST_BASENAME}.rc
        ${MC_GENERATED_DIR}/${MC_MANIFEST_BASENAME}TEMP.BIN
        ${MC_GENERATED_DIR}/${MC_MANIFEST_BASENAME}_MSG00001.BIN
        )

    SET(MC_COMMAND "mc.exe -h ${MC_GENERATED_DIR} -r ${MC_GENERATED_DIR} ${MC_MANIFEST_FULLNAME}")

    add_custom_command(
        COMMENT "Generating native event manifest files for ${EVENTS_MANIFEST}"
        OUTPUT ${MC_GENERATED_FILES}
        DEPENDS ${MC_MANIFEST_FULLNAME}
        COMMAND cmd.exe /c ${MC_COMMAND}
        WORKING_DIRECTORY ${MC_GENERATED_DIR}
        VERBATIM
        )

    list (APPEND GeneratedManifestFiles ${MC_GENERATED_DIR}/${MC_MANIFEST_BASENAME}.rc)

    set_source_files_properties(${GeneratedManifestFiles} PROPERTIES GENERATED TRUE)
    add_custom_target(GeneratedManifestFiles DEPENDS ${GeneratedManifestFiles})

    # for a resource only dll, cmake can report an error
    # if there is no linker language set.
    # CMake Error: Error required internal CMake variable not set, cmake may be not be built correctly.
    # Missing variable is: CMAKE_RC_CREATE_SHARED_LIBRARY
    get_property(isSet TARGET ${ARGV0} PROPERTY LINKER_LANGUAGE SET)
    if (NOT ${isSet})
        set_target_properties(${ARGV0} PROPERTIES LINKER_LANGUAGE "CXX")
    endif()

    set_target_properties(${ARGV0} PROPERTIES LINK_FLAGS "/NODEFAULTLIB /NOENTRY")

    if (BUILD_ONECORE)
        set_target_properties(${ARGV0} PROPERTIES COMPILE_DEFINITIONS "CORECLR")
    endif (BUILD_ONECORE)

    # ensure the target is dependent on the generated files.
    add_dependencies(${ARGV0} GeneratedManifestFiles)

endfunction()

file(GLOB SOURCES   ${CMAKE_CURRENT_SOURCE_DIR}/version.rc)

add_manifest_binary(PowerShell.Core.Instrumentation SHARED ${SOURCES})

