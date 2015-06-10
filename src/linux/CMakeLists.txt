cmake_minimum_required(VERSION 2.8.4)
project(linux)

set(CMAKE_CXX_COMPILER /usr/bin/clang++)

# generic settings
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++11 -stdlib=libc++")

# include folders from PS for Linux
include_directories(${PROJECT_SOURCE_DIR}/stubs)

# add coreclr
# - PLATFORM_UNIX is for pal headers
# - PAL_IMPLEMENTATION is for pal headers to define required function implementations (required for coreclr)
# - __LINUX__ defines the pal headers correctly define stuff for linux
# - COM_NO_WINDOWS_H defines that coreclr headers are not used on windows
# - CORECRL required for PowerShell code to enable CoreCRL support
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -DPLATFORM_UNIX -DPAL_IMPLEMENTATION -D__LINUX__ -DCOM_NO_WINDOWS_H -DCORECLR")
include_directories(${PROJECT_SOURCE_DIR}/../coreclr/src/pal/inc)
#include_directories(${PROJECT_SOURCE_DIR}/../coreclr/src/pal/prebuilt/inc)

# include folders from PS
include_directories(${PROJECT_SOURCE_DIR}/../monad/monad/nttargets/assemblies/nativemsh/pwrshcommon)

set(SOURCE_FILES main.cpp
  test/test_pal.cpp
  #../monad/monad/nttargets/assemblies/nativemsh/pwrshexe/CssMainEntry.cpp
)

add_executable(linux ${SOURCE_FILES})

# add cppunit
include_directories(${PROJECT_SOURCE_DIR}/../../externals/cppunit/include)
find_library(LIB_CPPUNIT cppunit ${PROJECT_SOURCE_DIR}/../../externals/cppunit/lib)
target_link_libraries(linux ${LIB_CPPUNIT})
message(STATUS "cppunit lib: ${LIB_CPPUNIT}")

