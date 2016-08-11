cmake_minimum_required(VERSION 2.8.4)

set(CMAKE_CXX_STANDARD_LIBRARIES "") # do not link against standard win32 libs i.e. kernel32, uuid, user32, etc.

if(NOT BUILD_ARCH_ARM64)
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /guard:cf")
    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /guard:cf")
endif (NOT BUILD_ARCH_ARM64)

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
set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /DEBUGTYPE:cv,fixup") #debugging format
set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /PDBCOMPRESS") #shrink pdb size
set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /DEBUG")

set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /DEBUG /PDBCOMPRESS")
#set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /STACK:1572864")

# Debug build specific flags
set(CMAKE_SHARED_LINKER_FLAGS_DEBUG "/NOVCFEATURE ${NO_INCREMENTAL_LINKER_FLAGS}")
set(CMAKE_SHARED_LINKER_FLAGS_DEBUG "${NO_INCREMENTAL_LINKER_FLAGS}")
set(CMAKE_EXE_LINKER_FLAGS_DEBUG "${NO_INCREMENTAL_LINKER_FLAGS}")

# Checked build specific flags
set(CMAKE_SHARED_LINKER_FLAGS_CHECKED "${CMAKE_SHARED_LINKER_FLAGS_CHECKED} /OPT:REF /OPT:NOICF /NOVCFEATURE ${NO_INCREMENTAL_LINKER_FLAGS}")
set(CMAKE_STATIC_LINKER_FLAGS_CHECKED "${CMAKE_STATIC_LINKER_FLAGS_CHECKED}")
set(CMAKE_EXE_LINKER_FLAGS_CHECKED "${CMAKE_EXE_LINKER_FLAGS_CHECKED} /OPT:REF /OPT:NOICF ${NO_INCREMENTAL_LINKER_FLAGS}")

# Release build specific flags
set(CMAKE_SHARED_LINKER_FLAGS_RELEASE "${CMAKE_SHARED_LINKER_FLAGS_RELEASE} /LTCG /OPT:REF /OPT:ICF ${NO_INCREMENTAL_LINKER_FLAGS}")
set(CMAKE_STATIC_LINKER_FLAGS_RELEASE "${CMAKE_STATIC_LINKER_FLAGS_RELEASE} /LTCG")
set(CMAKE_EXE_LINKER_FLAGS_RELEASE "${CMAKE_EXE_LINKER_FLAGS_RELEASE} /LTCG /OPT:REF /OPT:ICF ${NO_INCREMENTAL_LINKER_FLAGS}")

# ReleaseWithDebugInfo build specific flags
set(CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO "${CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO} /LTCG /OPT:REF /OPT:ICF ${NO_INCREMENTAL_LINKER_FLAGS}")
set(CMAKE_STATIC_LINKER_FLAGS_RELWITHDEBINFO "${CMAKE_STATIC_LINKER_FLAGS_RELWITHDEBINFO} /LTCG")
set(CMAKE_EXE_LINKER_FLAGS_RELWITHDEBINFO "${CMAKE_EXE_LINKER_FLAGS_RELWITHDEBINFO} /LTCG /OPT:REF /OPT:ICF ${NO_INCREMENTAL_LINKER_FLAGS}")

# Force uCRT to be dynamically linked for Release build  
set(CMAKE_SHARED_LINKER_FLAGS_RELEASE "${CMAKE_SHARED_LINKER_FLAGS_RELEASE} /NODEFAULTLIB:libucrt.lib /DEFAULTLIB:ucrt.lib")  
set(CMAKE_EXE_LINKER_FLAGS_RELEASE "${CMAKE_EXE_LINKER_FLAGS_RELEASE} /NODEFAULTLIB:libucrt.lib /DEFAULTLIB:ucrt.lib")  
set(CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO "${CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO} /NODEFAULTLIB:libucrt.lib /DEFAULTLIB:ucrt.lib")  
set(CMAKE_EXE_LINKER_FLAGS_RELWITHDEBINFO "${CMAKE_EXE_LINKER_FLAGS_RELWITHDEBINFO} /NODEFAULTLIB:libucrt.lib /DEFAULTLIB:ucrt.lib") 

#------------------------------------
# Definitions (for platform)
#-----------------------------------
if (BUILD_ARCH_AMD64)
    add_definitions(-D_AMD64_)
    add_definitions(-D_WIN64)
    add_definitions(-DAMD64)
    add_definitions(-DBIT64=1)
elseif (BUILD_ARCH_I386)
    add_definitions(-D_X86_)
elseif (BUILD_ARCH_ARM)
    add_definitions(-D_ARM_)
    add_definitions(-DARM)
elseif (BUILD_ARCH_ARM64)
    add_definitions(-D_ARM64_)
    add_definitions(-DARM64)
    add_definitions(-D_WIN64)
    add_definitions(-DBIT64=1)
endif ()

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
add_compile_options(/U_MT) # undefine the predefined _MT macro
add_compile_options(/GF) # enable read-only string pooling
add_compile_options(/Gm-) # disable minimal rebuild
add_compile_options(/EHa) # enable C++ EH (w/ SEH exceptions)
add_compile_options(/Zp8) # pack structs on 8-byte boundary
add_compile_options(/Gy) # separate functions for linker
add_compile_options(/Zc:wchar_t-) # C++ language conformance: wchar_t is NOT the native type, but a typedef
add_compile_options(/Zc:forScope) # C++ language conformance: enforce Standard C++ for scoping rules
add_compile_options(/GR-) # disable C++ RTTI
add_compile_options(/FC) # use full pathnames in diagnostics
if (BUILD_CORECLR)
    # This option is not supported for "FullClr" because it triggers: error C2813: #import is not supported with /MP
    add_compile_options(/MP) # Build with Multiple Processes (number of processes equal to the number of processors)
endif (BUILD_CORECLR)
add_compile_options(/GS) # Buffer Security Check
add_compile_options(/Zm200) # Specify Precompiled Header Memory Allocation Limit of 150MB
#add_compile_options(/wd4960 /wd4961 /wd4603 /wd4627 /wd4838 /wd4456 /wd4457 /wd4458 /wd4459 /wd4091 /we4640)
add_compile_options(/Zi) # enable debugging information
add_compile_options(/ZH:SHA_256) # use SHA256 for generating hashes of compiler processed source files.

if (BUILD_ARCH_I386)
   add_compile_options(/Gz)
endif (BUILD_ARCH_I386)

add_compile_options($<$<OR:$<CONFIG:Release>,$<CONFIG:Relwithdebinfo>>:/GL>)
add_compile_options($<$<OR:$<OR:$<CONFIG:Release>,$<CONFIG:Relwithdebinfo>>,$<CONFIG:Checked>>:/O1>)

if (BUILD_ARCH_AMD64)
# The generator expression in the following command means that the /homeparams option is added only for debug builds
    add_compile_options($<$<CONFIG:Debug>:/homeparams>) # Force parameters passed in registers to be written to the stack
endif (BUILD_ARCH_AMD64)

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

#set(CMAKE_ASM_MASM_FLAGS "${CMAKE_ASM_MASM_FLAGS} /ZH:SHA_256")

if (BUILD_ARCH_AMD64)
  add_definitions(-D_TARGET_AMD64_=1)
elseif (BUILD_ARCH_ARM)
  add_definitions(-D_TARGET_ARM_=1)
elseif (BUILD_ARCH_I386)
  add_definitions(-D_TARGET_X86_=1)
endif (BUILD_ARCH_AMD64)

add_definitions(-DWIN32)
add_definitions(-D_WIN32)
add_definitions(-DWINVER=${WIN_VERSION_WIN8})
add_definitions(-D_WIN32_WINNT=${WIN_VERSION_WIN8})
add_definitions(-DWIN32_LEAN_AND_MEAN=1)
add_definitions(-D_CRT_SECURE_NO_WARNINGS)

if(BUILD_ARCH_AMD64 OR BUILD_ARCH_I386)
    # Only enable edit and continue on windows x86 and x64.
    # Exclude arm & arm64
    add_definitions(-DEnC_SUPPORTED)
endif(BUILD_ARCH_AMD64 OR BUILD_ARCH_I386)

add_definitions(-DUNICODE)
add_definitions(-D_UNICODE)

