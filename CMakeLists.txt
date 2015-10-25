project(MONAD_NATIVE)

add_compile_options(-std=c++11 -Wall)

add_subdirectory(impl)
add_subdirectory(host)
add_subdirectory(tests)
