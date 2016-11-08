cmake_minimum_required(VERSION 2.8.11)
project(PSL-NATIVE)

# Can't use add_compile_options with 2.8.11
set(CMAKE_BUILD_TYPE "Release")
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++11 -Wall -Werror")
set(LIBRARY_OUTPUT_PATH "${PROJECT_SOURCE_DIR}/../powershell-unix")

# test in BUILD_DIR
enable_testing()
add_subdirectory(src)
add_subdirectory(test)
