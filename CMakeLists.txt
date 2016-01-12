cmake_minimum_required(VERSION 2.8.4)
project(MONAD_NATIVE)

add_compile_options(-std=c++11 -Wall -Werror)

# test in BUILD_DIR
enable_testing()
add_subdirectory(src)
add_subdirectory(tests)
