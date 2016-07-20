cmake_minimum_required(VERSION 2.8.4)

SET (WindowsSdkDir $ENV{WindowsSdkDir})
SET (WindowsSDKVersion $ENV{WindowsSDKVersion})
SET (NETFXSdkDir $ENV{NETFXSDKDir})

#
# Configure include directories
#
SET (WindowsSDKIncludeBase "${WindowsSdkDir}/Include/${WindowsSDKVersion}")

SET (IncludePath)
list (APPEND IncludePath "${WindowsSDKIncludeBase}winrt")
list (APPEND IncludePath "${WindowsSDKIncludeBase}shared")
#list (APPEND IncludePath "${NETFXSdkDir}/Include/um")
list (APPEND IncludePath "${WindowsSDKIncludeBase}ucrt")
include_directories(BEFORE ${IncludePath})

#
# Configure lib directories
#
SET (WindowsSDKLibBase "${WindowsSdkDir}/Lib/${WindowsSDKVersion}")
SET (OneCoreLibBase "$ENV{VCInstallDir}lib/onecore/amd64")

SET (LibraryPath)
list (APPEND LibraryPath "${OneCoreLibBase}")
list (APPEND LibraryPath "${WindowsSDKLibBase}ucrt/${WindowsSDKPlatform}")
list (APPEND LibraryPath "${WindowsSDKLibBase}um/${WindowsSDKPlatform}" )
###list (APPEND LibraryPath "${NETFXSdkDir}lib/um/${WindowsSDKPlatform}")
link_directories(${LibraryPath})

#
# Tell CMake to set the platform toolset. Nano Server requires the Win10 SDK and updated onecore.lib
#
set(CMAKE_VS_PLATFORM_TOOLSET "v140") # Use VS 2015 with Win 10 SDK
set(CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION "10.0.10586.0") # Targets Windows 10. Alt is ${WindowsSDKVersion}

if (BUILD_ONECORE)
    set(CMAKE_CXX_STANDARD_LIBRARIES "") # do not link against standard win32 libs i.e. kernel32, uuid, user32, etc.
endif (BUILD_ONECORE)

add_compile_options(/Zl) # omit default library name in .OBJ
add_compile_options(/Zi) # enable debugging information
add_compile_options(/nologo) # Suppress Startup Banner
add_compile_options(/W3) # set warning level to 3
#add_compile_options(/WX-) # treat warnings as errors
add_compile_options(/wd4996) # Ignore deprecation warnings
add_compile_options(/Od) # enable intrinsics
add_compile_options(/sdl)

add_compile_options(/Gm) # minimal rebuild
add_compile_options(/EHsc) # enable C++ EH (w/ SEH exceptions)
add_compile_options(/RTC1)
#add_compile_options(/MDd)
add_compile_options(/MD)
add_compile_options(/GS) # Buffer Security Check
add_compile_options(/fp:precise)
add_compile_options(/Zp8) # pack structs on 8-byte boundary
add_compile_options(/Zc:wchar_t) # C++ language conformance: wchar_t is NOT the native type, but a typedef
#add_compile_options(/U_WINDOWS)

add_definitions(
    -D_WIN64
    -D_AMD64_
    -DAMD64
    -D_APISET_WINDOWS_VERSION=0x601
    -D_APISET_MINWIN_VERSION=0x0101
    -D_APISET_MINCORE_VERSION=0x0100
    -DNTDDI_VERSION=0x0A000002
    #    -DWIN32=100
    -D_DEBUG
    -D_UNICODE
    -DUNICODE
    -DWIN32_LEAN_AND_MEAN=1
    #-DNDEBUG
    )

set(CMAKE_ENABLE_EXPORTS ON)

set(MY_COMMON_LINK_FLAGS "/NOLOGO /MANIFEST:NO /NXCOMPAT /DYNAMICBASE /TLBID:1 /MACHINE:x64 /guard:cf /OPT:REF /OPT:ICF /NODEFAULTLIB")
set(MY_COMMON_LINK_FLAGS "${MY_COMMON_LINK_FLAGS} /NODEFAULTLIB:kernel32.lib /NODEFAULTLIB:advapi32.lib") # Explicitly exclude kernel32 and advapi32 since CMake is including them and they block execution on Nano Server

set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} ${MY_COMMON_LINK_FLAGS}")
set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /SUBSYSTEM:WINDOWS,6.00 /INCREMENTAL:NO") #windows subsystem

set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} ${MY_COMMON_LINK_FLAGS}")
set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /SUBSYSTEM:CONSOLE /INCREMENTAL:NO") #windows subsystem

