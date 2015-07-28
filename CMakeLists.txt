cmake_minimum_required(VERSION 2.8.4)
project(monad_native)

set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++11")

#include gtest
include_directories(../ext-src/gtest/fused-src impl)
link_directories(${monad_native_BINARY_DIR})

# source file definitions
set(LIB_SOURCE_FILES impl/getcurrentprocessorid.cpp impl/getusername.cpp impl/terminal.cpp)
set(TEST_SOURCE_FILES tests/test-getcurrentprocessid.cpp)
set(SOURCE_FILES main.cpp ../ext-src/gtest/fused-src/gtest/gtest-all.cc)
SET(HOST_CMDLINE_SOURCE_FILES host/cmdline/main.cpp host/cmdline/coreclrutil.cpp)

# target definitions
add_library(ps SHARED ${LIB_SOURCE_FILES})
add_executable(monad_native ${SOURCE_FILES} ${TEST_SOURCE_FILES})
add_executable(host_cmdline ${HOST_CMDLINE_SOURCE_FILES})

# add pthread and other libs
find_package(Threads)
target_link_libraries(monad_native ${CMAKE_THREAD_LIBS_INIT} ps)
target_link_libraries(host_cmdline dl icuuc)



