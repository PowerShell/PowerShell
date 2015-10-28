cmake_minimum_required(VERSION 2.8.4)
project(MONAD_NATIVE)

add_compile_options(-std=c++11 -Wall)

# test in BUILD_DIR
enable_testing()
add_subdirectory(tests)
add_subdirectory(native)
add_subdirectory(host)
