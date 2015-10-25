project(MONAD_NATIVE)

add_compile_options(-std=c++11 -Wall)

add_subdirectory(native)
add_subdirectory(host)
add_subdirectory(tests)
