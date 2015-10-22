cmake_minimum_required(VERSION 2.8.4)
project(monad_native)

set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++11 -Wall")

#include gtest
include_directories(../ext-src/gtest/fused-src impl)
link_directories(${monad_native_BINARY_DIR})

# source file definitions
set(LIB_SOURCE_FILES impl/getcurrentprocessorid.cpp impl/getusername.cpp impl/terminal.cpp impl/getcomputername.cpp impl/getcpinfo.cpp)
set(HOST_COMMON_SOURCE_FILES host/common/coreclrutil.cpp)
set(HOST_COMMON_TEST_SOURCE_FILES tests/host/test-hostutil.cpp)
set(TEST_SOURCE_FILES tests/test-getcurrentprocessid.cpp tests/test-getusername.cpp tests/test-getcomputername.cpp tests/test-getcpinfo.cpp ${HOST_COMMON_SOURCE_FILES} ${HOST_COMMON_TEST_SOURCE_FILES})
set(SOURCE_FILES main.cpp ../ext-src/gtest/fused-src/gtest/gtest-all.cc)
set(HOST_CMDLINE_SOURCE_FILES host/cmdline/main.cpp)

# target definitions
add_library(ps SHARED ${LIB_SOURCE_FILES})
add_executable(monad_native ${SOURCE_FILES} ${TEST_SOURCE_FILES})
add_library(pshost STATIC ${HOST_COMMON_SOURCE_FILES})
add_executable(host_cmdline ${HOST_CMDLINE_SOURCE_FILES})

# add pthread and other libs
find_package(Threads)
target_link_libraries(ps icuuc)
target_link_libraries(monad_native ${CMAKE_THREAD_LIBS_INIT} ps pshost)
target_link_libraries(pshost dl icuuc)
target_link_libraries(host_cmdline pshost icuuc)

# target specific include directories
target_include_directories(monad_native PRIVATE host)
target_include_directories(host_cmdline PRIVATE host)
