cmake_minimum_required(VERSION 2.8.4)
project(linux)

# generic settings
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++11")

# add coreclr
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -DPLATFORM_UNIX -DPAL_IMPLEMENTATION -D__LINUX__")
include_directories(${root_SOURCE_DIR}../coreclr/src/pal/inc)

set(SOURCE_FILES main.cpp
  test/test_pal.cpp
#  ../monad/monad/nttargets/assemblies/nativemsh/pwrshexe/CssMainEntry.cpp
)

add_executable(linux ${SOURCE_FILES})

# add cppunit
include_directories(${root_SOURCE_DIR}../../externals/cppunit/include)
find_library(LIB_CPPUNIT cppunit ${root_SOURCE_DIR}../../externals/cppunit/lib)
target_link_libraries(linux ${LIB_CPPUNIT})
message(STATUS "cppunit lib: ${LIB_CPPUNIT}")

