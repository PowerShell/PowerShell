cmake_minimum_required(VERSION 2.8.4)

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
SET (OneCoreLibBase "$ENV{VCInstallDir}lib/onecore/amd64")

SET (LibraryPath)
if (BUILD_ONECORE)
    list (APPEND LibraryPath "${OneCoreLibBase}")
endif (BUILD_ONECORE)
list (APPEND LibraryPath "${WindowsSDKLibBase}ucrt/${WindowsSDKPlatform}")
list (APPEND LibraryPath "${NETFXSdkDir}lib/um/${WindowsSDKPlatform}")
list (APPEND LibraryPath "${WindowsSDKLibBase}um/${WindowsSDKPlatform}" )
list (APPEND LibraryPath "${INTERNAL_LIBRARY_DIR}")
list (APPEND LibraryPath "${PUBLIC_LIBRARY_DIR}")
#list (APPEND LibraryPath ${FrameWorkLibPath})
SET (WindowsSDKLibBase "${WindowsSdkDir}/Lib/${WindowsSDKVersion}")
link_directories(${LibraryPath})

if (${WindowsSDKPlatform} STREQUAL "x64")
    add_definitions (
        -D_WIN64
        -D_AMD64_
        -DAMD64
        -DBUILD_WOW64_ENABLED=1
        -DBUILD_UMS_ENABLED=1
    )
else()
    add_definitions (
        -DBUILD_WOW64_ENABLED=1
        -DBUILD_UMS_ENABLED=0
    )
endif()

#
# Common defines.
#
add_definitions (
    -D_CRT_SECURE_NO_WARNINGS
    -DCONDITION_HANDLING=1
    -DNT_UP=1
    -DNT_INST=0
    -D_NT1X_=100
    -DWINNT=1
    -DWIN32_LEAN_AND_MEAN=1
    -DDEVL=1
    #-D_MT=1
    -DMD
    -D_STL70_
    -DMI_INTERNAL
    -DWINBUILD
    -DHOOK_BUILD # TODO: should be target specific
    -DCONFIG_ENABLE_WCHAR
    -D_INTLSTR_NOTAPPEND_NULL
    -DMSC_NOOPT
    -DBUILD_WINDOWS
    -D_USE_DECLSPECS_FOR_SAL=1
    -DUNICODE
    -D_UNICODE
    -D_USE_DEV11_CRT
)

#
# platform specific defines
#
add_definitions(
    #-DNTDDI_VERSION=${NTDDI_VERSION_WIN7}
    -DNTDDI_VERSION=${NTDDI_VERSION_WIN10}
    -DWINBLUE_KBSPRING14
    #-D_APISET_WINDOWS_VERSION=${WIN_VERSION_WIN7}
    -D_APISET_WINDOWS_VERSION=${WIN_VERSION_WIN10}
    #-D_APISET_MINWIN_VERSION=0x0100
    -D_APISET_MINWIN_VERSION=0x0106
    #-D_APISET_MINCORE_VERSION=0x0100
    -D_APISET_MINCORE_VERSION=0x0105
    -D_WIN32_IE=0x0800
    #-D_WIN32_WINNT=${WIN_VERSION_WIN7}
    -D_WIN32_WINNT=${WIN_VERSION_WIN10}
    #-DWINVER=${WIN_VERSION_WIN7}
    -DWINVER=${WIN_VERSION_WIN10}
    )

# if not DEBUG for DSC
set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} /Zi")
set(CMAKE_SHARED_LINKER_FLAGS_RELEASE "${CMAKE_SHARED_LINKER_FLAGS_RELEASE} /DEBUG /OPT:REF /OPT:ICF")
#set(CMAKE_SHARED_LINKER_FLAGS_RELEASE "${CMAKE_SHARED_LINKER_FLAGS_RELEASE} /DEBUG /OPT:REF /OPT:ICF /NODEFAULTLIB")
set(CMAKE_EXE_LINKER_FLAGS_RELEASE "${CMAKE_EXE_LINKER_FLAGS_RELEASE} /DEBUG /OPT:REF /OPT:ICF /NODEFAULTLIB")

