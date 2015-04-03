cmake_minimum_required(VERSION 2.8.4)
project(monad_native)

set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++11")

#include gtest
include_directories(../ext-src/gtest/fused-src impl)

set(SOURCE_FILES main.cpp tests/test-getcurrentprocessid.cpp impl/getcurrentprocessorid.cpp tests/test-getcomputername.cpp impl/getcomputername.cpp
../ext-src/gtest/fused-src/gtest/gtest-all.cc)
add_executable(monad_native ${SOURCE_FILES})

# add pthread
find_package(Threads)
target_link_libraries(monad_native ${CMAKE_THREAD_LIBS_INIT})



