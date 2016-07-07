cmake_minimum_required(VERSION 2.8.4)
project(PowerShellNative)

#
# Verify prerequisites
#
if (NOT $ENV{${WindowsSDKVersion}})
    message (FATAL_ERROR "WindowsSDKVersion environment variable not found")
endif ()

if (NOT $ENV{${Platform}})
    message(FATAL_ERROR "Platform environment variable not found")
else ()
    SET(WindowsSDKPlatform "$ENV{Platform}")
endif ()

#
# Get the path to the Windows SDK build tools.
#
if (WindowsSDKPlatform STREQUAL "x64" OR WindowsSDKPlatform STREQUAL "X64" OR WindowsSDKPlatform STREQUAL "amd64" OR WindowsSDKPlatform STREQUAL "AMD64")
    # Normalize the platform name
    SET(WindowsSDKPlatform "x64")
elseif (WindowsSDKPlatform STREQUAL "x86" OR WindowsSDKPlatform STREQUAL "X86")
    # Normalize the platform name
    SET(WindowsSDKPlatform "x86")
else()
    message(FATAL_ERROR "Unsupported WindowsSDKPlatform ${WindowsSDKPlatform}")
endif ()

#
# set the output path for all binaries
#
if (BUILD_ONECORE)
    set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${PROJECT_SOURCE_DIR}/Bin/CoreClr")
else ()
    set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${PROJECT_SOURCE_DIR}/Bin/FullClr")
endif (BUILD_ONECORE)
foreach(OUTPUTCONFIG ${CMAKE_CONFIGURATION_TYPES})
    string(TOUPPER ${OUTPUTCONFIG} OUTPUTCONFIG)
    set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_${OUTPUTCONFIG} ${CMAKE_RUNTIME_OUTPUT_DIRECTORY})
    message("  Setting output directory for ${OUTPUTCONFIG} to ${CMAKE_RUNTIME_OUTPUT_DIRECTORY}")
endforeach( OUTPUTCONFIG CMAKE_CONFIGURATION_TYPES )

#
# Definitions for ease of reading
#
SET (WIN_VERSION_WIN10_RS1     0x0A000002)
SET (WIN_VERSION_WIN10_TH2     0x0A000001)
SET (WIN_VERSION_WIN10         0x0A00)
SET (WIN_VERSION_WINTHRESHOLD  0x0A00)
SET (WIN_VERSION_WINBLUE       0x0603)
SET (WIN_VERSION_WIN8          0x0602)
SET (WIN_VERSION_WIN7          0x0601)
SET (NTDDI_VERSION_WIN7       0x06010000)
SET (WIN_VERSION_VISTA         0x0600)
SET (WIN_VERSION_LONGHORN      0x0600)
SET (WIN_VERSION_WS03          0x0502)
SET (WIN_VERSION_WINXP         0x0501)

# ***
# *** Original flags: BEGIN
# ***
#
# TODO: Will this interfere with OneCore compilations?
#
# set these flags, so build does static linking for msvcr120.dll
# otherwise this dll need to be present on the system
#set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} /MT")
#set(CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG} /MTd")

# Compiler flags that are universal to all powershell native binaries
#SET(common_pwrsh_definitions 
#    UNICODE
#    _UNICODE)

#set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /VERBOSE")

# ***
# *** Original flags: END
# ***

# ***
# *** CoreCLR flags: BEGIN
# ***

set(CMAKE_CXX_STANDARD_LIBRARIES "") # do not link against standard win32 libs i.e. kernel32, uuid, user32, etc.

#if(NOT CLR_CMAKE_PLATFORM_ARCH_ARM64)
  set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /guard:cf")
  set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /guard:cf")
#endif (NOT CLR_CMAKE_PLATFORM_ARCH_ARM64)

# Incremental linking with CFG is broken until next VS release.
# This needs to be appended to the last for each build type to override the default flag.
set(NO_INCREMENTAL_LINKER_FLAGS "/INCREMENTAL:NO")

# Linker flags
#
# Disable the following line for UNIX altjit on Windows
set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /MANIFEST:NO") #Do not create Side-by-Side Assembly Manifest
set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /SUBSYSTEM:WINDOWS,6.00") #windows subsystem
set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /LARGEADDRESSAWARE") # can handle addresses larger than 2 gigabytes
set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /RELEASE") #sets the checksum in the header
set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /NXCOMPAT") #Compatible with Data Execution Prevention
set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /DYNAMICBASE") #Use address space layout randomization
#set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /DEBUGTYPE:cv,fixup") #debugging format
set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /PDBCOMPRESS") #shrink pdb size
set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /DEBUG")

#
# TODO: Should these be ignored????
#
#set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /IGNORE:4197,4013,4254,4070,4221")
#set(CMAKE_STATIC_LINKER_FLAGS "${CMAKE_STATIC_LINKER_FLAGS} /IGNORE:4221")

set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /DEBUG /PDBCOMPRESS")
#set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /STACK:1572864")

# Debug build specific flags
#set(CMAKE_SHARED_LINKER_FLAGS_DEBUG "/NOVCFEATURE ${NO_INCREMENTAL_LINKER_FLAGS}")
set(CMAKE_SHARED_LINKER_FLAGS_DEBUG "${NO_INCREMENTAL_LINKER_FLAGS}")
set(CMAKE_EXE_LINKER_FLAGS_DEBUG "${NO_INCREMENTAL_LINKER_FLAGS}")

# Checked build specific flags
#set(CMAKE_SHARED_LINKER_FLAGS_CHECKED "${CMAKE_SHARED_LINKER_FLAGS_CHECKED} /OPT:REF /OPT:NOICF /NOVCFEATURE ${NO_INCREMENTAL_LINKER_FLAGS}")
#set(CMAKE_STATIC_LINKER_FLAGS_CHECKED "${CMAKE_STATIC_LINKER_FLAGS_CHECKED}")
#set(CMAKE_EXE_LINKER_FLAGS_CHECKED "${CMAKE_EXE_LINKER_FLAGS_CHECKED} /OPT:REF /OPT:NOICF ${NO_INCREMENTAL_LINKER_FLAGS}")

# Release build specific flags
#set(CMAKE_SHARED_LINKER_FLAGS_RELEASE "${CMAKE_SHARED_LINKER_FLAGS_RELEASE} /LTCG /OPT:REF /OPT:ICF ${NO_INCREMENTAL_LINKER_FLAGS}")
#set(CMAKE_STATIC_LINKER_FLAGS_RELEASE "${CMAKE_STATIC_LINKER_FLAGS_RELEASE} /LTCG")
#set(CMAKE_EXE_LINKER_FLAGS_RELEASE "${CMAKE_EXE_LINKER_FLAGS_RELEASE} /LTCG /OPT:REF /OPT:ICF ${NO_INCREMENTAL_LINKER_FLAGS}")

# ReleaseWithDebugInfo build specific flags
#set(CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO "${CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO} /LTCG /OPT:REF /OPT:ICF ${NO_INCREMENTAL_LINKER_FLAGS}")
#set(CMAKE_STATIC_LINKER_FLAGS_RELWITHDEBINFO "${CMAKE_STATIC_LINKER_FLAGS_RELWITHDEBINFO} /LTCG")
#set(CMAKE_EXE_LINKER_FLAGS_RELWITHDEBINFO "${CMAKE_EXE_LINKER_FLAGS_RELWITHDEBINFO} /LTCG /OPT:REF /OPT:ICF ${NO_INCREMENTAL_LINKER_FLAGS}")

# Temporary until cmake has VS generators for arm64
#if(CLR_CMAKE_PLATFORM_ARCH_ARM64)
#    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /machine:arm64")
#    set(CMAKE_STATIC_LINKER_FLAGS "${CMAKE_STATIC_LINKER_FLAGS} /machine:arm64")
#    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /machine:arm64")
#endif(CLR_CMAKE_PLATFORM_ARCH_ARM64)

# Force uCRT to be dynamically linked for Release build  
#set(CMAKE_SHARED_LINKER_FLAGS_RELEASE "${CMAKE_SHARED_LINKER_FLAGS_RELEASE} /NODEFAULTLIB:libucrt.lib /DEFAULTLIB:ucrt.lib")  
#set(CMAKE_EXE_LINKER_FLAGS_RELEASE "${CMAKE_EXE_LINKER_FLAGS_RELEASE} /NODEFAULTLIB:libucrt.lib /DEFAULTLIB:ucrt.lib")  
#set(CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO "${CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO} /NODEFAULTLIB:libucrt.lib /DEFAULTLIB:ucrt.lib")  
#set(CMAKE_EXE_LINKER_FLAGS_RELWITHDEBINFO "${CMAKE_EXE_LINKER_FLAGS_RELWITHDEBINFO} /NODEFAULTLIB:libucrt.lib /DEFAULTLIB:ucrt.lib") 

#------------------------------------
# Definitions (for platform)
#-----------------------------------
#if (CLR_CMAKE_PLATFORM_ARCH_AMD64)
  add_definitions(-D_AMD64_)
  add_definitions(-D_WIN64)
  add_definitions(-DAMD64)
  add_definitions(-DBIT64=1)
#elseif (CLR_CMAKE_PLATFORM_ARCH_I386)
#  add_definitions(-D_X86_)
#elseif (CLR_CMAKE_PLATFORM_ARCH_ARM)
#  add_definitions(-D_ARM_)
#  add_definitions(-DARM)
#elseif (CLR_CMAKE_PLATFORM_ARCH_ARM64)
#  add_definitions(-D_ARM64_)
#  add_definitions(-DARM64)
#  add_definitions(-D_WIN64)
#  add_definitions(-DBIT64=1)
#else ()
#  clr_unknown_arch()
#endif ()

# Define the CRT lib references that link into Desktop imports
set(STATIC_MT_CRT_LIB  "libcmt$<$<OR:$<CONFIG:Debug>,$<CONFIG:Checked>>:d>.lib")
set(STATIC_MT_VCRT_LIB  "libvcruntime$<$<OR:$<CONFIG:Debug>,$<CONFIG:Checked>>:d>.lib")
set(STATIC_MT_CPP_LIB  "libcpmt$<$<OR:$<CONFIG:Debug>,$<CONFIG:Checked>>:d>.lib")

# Define the uCRT lib reference
set(STATIC_UCRT_LIB  "libucrt$<$<OR:$<CONFIG:Debug>,$<CONFIG:Checked>>:d>.lib")
set(DYNAMIC_UCRT_LIB  "ucrt$<$<OR:$<CONFIG:Debug>,$<CONFIG:Checked>>:d>.lib")

#------------------------------------
# Definitions for compile
#-----------------------------------

add_compile_options(/Zl) # omit default library name in .OBJ

# The following options are set by the razzle build
add_compile_options(/TP) # compile all files as C++
add_compile_options(/d2Zi+) # make optimized builds debugging easier
add_compile_options(/nologo) # Suppress Startup Banner
add_compile_options(/W3) # set warning level to 3
#add_compile_options(/WX) # treat warnings as errors
add_compile_options(/Oi) # enable intrinsics
add_compile_options(/Oy-) # disable suppressing of the creation of frame pointers on the call stack for quicker function calls
#add_compile_options(/U_MT) # undefine the predefined _MT macro
add_compile_options(/GF) # enable read-only string pooling
add_compile_options(/Gm-) # disable minimal rebuild
add_compile_options(/EHa) # enable C++ EH (w/ SEH exceptions)
add_compile_options(/Zp8) # pack structs on 8-byte boundary
add_compile_options(/Gy) # separate functions for linker
add_compile_options(/Zc:wchar_t-) # C++ language conformance: wchar_t is NOT the native type, but a typedef
add_compile_options(/Zc:forScope) # C++ language conformance: enforce Standard C++ for scoping rules
add_compile_options(/GR-) # disable C++ RTTI
add_compile_options(/FC) # use full pathnames in diagnostics
#add_compile_options(/MP) # Build with Multiple Processes (number of processes equal to the number of processors)
add_compile_options(/GS) # Buffer Security Check
add_compile_options(/Zm200) # Specify Precompiled Header Memory Allocation Limit of 150MB
add_compile_options(/wd4960 /wd4961 /wd4603 /wd4627 /wd4838 /wd4456 /wd4457 /wd4458 /wd4459 /wd4091 /we4640)
add_compile_options(/Zi) # enable debugging information
add_compile_options(/ZH:SHA_256) # use SHA256 for generating hashes of compiler processed source files.

#if (CLR_CMAKE_PLATFORM_ARCH_I386)
#   add_compile_options(/Gz)
#endif (CLR_CMAKE_PLATFORM_ARCH_I386)

add_compile_options($<$<OR:$<CONFIG:Release>,$<CONFIG:Relwithdebinfo>>:/GL>)
add_compile_options($<$<OR:$<OR:$<CONFIG:Release>,$<CONFIG:Relwithdebinfo>>,$<CONFIG:Checked>>:/O1>)

#if (CLR_CMAKE_PLATFORM_ARCH_AMD64)
# The generator expression in the following command means that the /homeparams option is added only for debug builds
    add_compile_options($<$<CONFIG:Debug>:/homeparams>) # Force parameters passed in registers to be written to the stack
#endif (CLR_CMAKE_PLATFORM_ARCH_AMD64)

# enable control-flow-guard support for native components for non-Arm64 builds
add_compile_options(/guard:cf) 

# Statically linked CRT (libcmt[d].lib, libvcruntime[d].lib and libucrt[d].lib) by default. This is done to avoid  
# linking in VCRUNTIME140.DLL for a simplified xcopy experience by reducing the dependency on VC REDIST.  
#  
# For Release builds, we shall dynamically link into uCRT [ucrtbase.dll] (which is pushed down as a Windows Update on downlevel OS) but  
# wont do the same for debug/checked builds since ucrtbased.dll is not redistributable and Debug/Checked builds are not  
# production-time scenarios.  
add_compile_options($<$<OR:$<CONFIG:Release>,$<CONFIG:Relwithdebinfo>>:/MT>)  
add_compile_options($<$<OR:$<CONFIG:Debug>,$<CONFIG:Checked>>:/MTd>)  

set(CMAKE_ASM_MASM_FLAGS "${CMAKE_ASM_MASM_FLAGS} /ZH:SHA_256")

#if (CLR_CMAKE_TARGET_ARCH_AMD64)
  add_definitions(-D_TARGET_AMD64_=1)
  add_definitions(-DDBG_TARGET_64BIT=1)
  add_definitions(-DDBG_TARGET_AMD64=1)
  add_definitions(-DDBG_TARGET_WIN64=1)
#elseif (CLR_CMAKE_TARGET_ARCH_ARM)
#  add_definitions(-D_TARGET_ARM_=1)
#  add_definitions(-DDBG_TARGET_32BIT=1)
#  add_definitions(-DDBG_TARGET_ARM=1)
#elseif (CLR_CMAKE_TARGET_ARCH_I386)
#  add_definitions(-D_TARGET_X86_=1)
#  add_definitions(-DDBG_TARGET_32BIT=1)
#  add_definitions(-DDBG_TARGET_X86=1)
#else ()
#  clr_unknown_arch()
#endif (CLR_CMAKE_TARGET_ARCH_AMD64)

#
# TODO: Are these needed???????????
#
#add_definitions(-D_BLD_CLR)
#add_definitions(-DDEBUGGING_SUPPORTED)
#add_definitions(-DPROFILING_SUPPORTED)

add_definitions(-DWIN32)
add_definitions(-D_WIN32)
add_definitions(-DWINVER=${WIN_VERSION_WIN7})# 0x0601 for Win7, was 0x0602
add_definitions(-D_WIN32_WINNT=${WIN_VERSION_WIN7}) # 0x0601 for Win7
add_definitions(-DWIN32_LEAN_AND_MEAN=1)
add_definitions(-D_CRT_SECURE_NO_WARNINGS)
#if(CLR_CMAKE_TARGET_ARCH_AMD64 OR CLR_CMAKE_TARGET_ARCH_I386)
    # Only enable edit and continue on windows x86 and x64
    # exclude Linux, arm & arm64
    add_definitions(-DEnC_SUPPORTED)
#endif(CLR_CMAKE_TARGET_ARCH_AMD64 OR CLR_CMAKE_TARGET_ARCH_I386)

add_definitions(-DUNICODE)
add_definitions(-D_UNICODE)

# ***
# *** CoreCLR flags: END
# ***

# ***
# *** DSC flags: BEGIN
# ***


SET (WindowsSdkDir $ENV{WindowsSdkDir})
SET (WindowsSDKVersion $ENV{WindowsSDKVersion})
SET (NETFXSdkDir $ENV{NETFXSDKDir})
#SET (FrameWorkLibPath $ENV{FrameworkDir}/$ENV{FrameworkVersion})

#
# Configure include directories
#
SET (WindowsSDKIncludeBase "${WindowsSdkDir}/Include/${WindowsSDKVersion}")

SET (IncludePath)
#list (APPEND IncludePath "${INTERNAL_HEADER_DIR}")
#list (APPEND IncludePath "${PUBLIC_HEADER_DIR}")
list (APPEND IncludePath "${WindowsSDKIncludeBase}winrt")
# Don't include due to incompatible instance.h
# list (APPEND IncludePath "${WindowsSDKIncludeBase}um")
list (APPEND IncludePath "${WindowsSDKIncludeBase}shared")
list (APPEND IncludePath "${NETFXSdkDir}/Include/um")
list (APPEND IncludePath "${WindowsSDKIncludeBase}ucrt")
include_directories(BEFORE ${IncludePath})

#
# Configure lib directories
#
SET (WindowsSDKLibBase "${WindowsSdkDir}/Lib/${WindowsSDKVersion}")

SET (LibraryPath)
list (APPEND LibraryPath "${WindowsSDKLibBase}ucrt/${WindowsSDKPlatform}")
list (APPEND LibraryPath "${NETFXSdkDir}lib/um/${WindowsSDKPlatform}")
list (APPEND LibraryPath "${WindowsSDKLibBase}um/${WindowsSDKPlatform}" )
list (APPEND LibraryPath "${INTERNAL_LIBRARY_DIR}")
list (APPEND LibraryPath "${PUBLIC_LIBRARY_DIR}")
#list (APPEND LibraryPath ${FrameWorkLibPath})
SET (WindowsSDKLibBase "${WindowsSdkDir}/Lib/${WindowsSDKVersion}")
link_directories(${LibraryPath})

#if (${WindowsSDKPlatform} STREQUAL "x64")
#    add_definitions (
#        -D_WIN64
#        -D_AMD64_
#        -DAMD64
#        -DBUILD_WOW64_ENABLED=1
#        -DBUILD_UMS_ENABLED=1
#    )
#else()
#    add_definitions (
#        -DBUILD_WOW64_ENABLED=1
#        -DBUILD_UMS_ENABLED=0
#    )
#endif()

#
# Common defines.
#
#add_definitions (
#    -D_CRT_SECURE_NO_WARNINGS
#    -DCONDITION_HANDLING=1
#    -DNT_UP=1
#    -DNT_INST=0
#    -D_NT1X_=100
#    -DWINNT=1
#    -DWIN32_LEAN_AND_MEAN=1
#    -DDEVL=1
#    -D_MT=1
#    -D_STL70_
#    -DMI_INTERNAL
#    -DWINBUILD
#    -DHOOK_BUILD # TODO: should be target specific
#    -DCONFIG_ENABLE_WCHAR
#    -D_INTLSTR_NOTAPPEND_NULL
#    -DMSC_NOOPT
#    -DBUILD_WINDOWS
#    -D_USE_DECLSPECS_FOR_SAL=1
#    -DUNICODE
#    -D_UNICODE
#    -D_USE_DEV11_CRT
#)

#
# platform specific defines
#
#add_definitions(
#    -DNTDDI_VERSION=${NTDDI_VERSION_WIN7}
#    -DWINBLUE_KBSPRING14
#    -D_APISET_WINDOWS_VERSION=${WIN_VERSION_WIN7}
#    -D_APISET_MINWIN_VERSION=0x0100
#    -D_APISET_MINCORE_VERSION=0x0100
#    -D_WIN32_IE=0x0800
#    -D_WIN32_WINNT=${WIN_VERSION_WIN7}
#    -DWINVER=${WIN_VERSION_WIN7}
#    )

# ***
# *** DSC flags: END
# ***

# Default of BUILD_ONECORE should be ON once it is supported 
option(BUILD_ONECORE "Compile the OneCore version of the binaries" OFF)
option(BUILD_DOWNLEVEL "Compile the downlevel version of the binaries" OFF)

# Build the common library that powershell.exe and pwrshplugin.dll depend on
add_subdirectory(nativemsh/pwrshcommon)

# Build pwrshplugin.dll
add_subdirectory(nativemsh/pwrshplugin)

# Build powershell.exe
if (NOT BUILD_ONECORE)
    # Conditionally build powershell.exe until CssMainEntry.cpp appears.
    add_subdirectory(nativemsh/pwrshexe)
endif ()
