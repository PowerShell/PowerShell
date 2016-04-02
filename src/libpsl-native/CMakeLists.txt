cmake_minimum_required(VERSION 2.8.4)
project(PSL-NATIVE)

add_compile_options(-std=c++11 -Wall -Werror)
set(LIBRARY_OUTPUT_PATH "${PROJECT_SOURCE_DIR}/../Microsoft.PowerShell.Linux.Host")

# test in BUILD_DIR
enable_testing()
add_subdirectory(src)
add_subdirectory(test)
