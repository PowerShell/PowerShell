cmake_minimum_required(VERSION 2.8.4)
project(MONAD_NATIVE)

add_compile_options(-std=c++11 -Wall)

# resolve superproject directory
set(MONAD ${CMAKE_SOURCE_DIR}/../../)

# test in BUILD_DIR
enable_testing()
add_subdirectory(tests)

# output psnative and pshost to superproject
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${MONAD}/bin)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${MONAD}/lib)
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${MONAD}/lib)

add_subdirectory(native)
add_subdirectory(host)
