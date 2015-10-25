add_subdirectory(googletest)

add_executable(monad_native
  main.cpp
  test-getcurrentprocessid.cpp
  test-getusername.cpp
  test-getcomputername.cpp)

target_include_directories(monad_native PRIVATE ${gtest_SOURCE_DIR}/include)

find_package(Threads)
target_link_libraries(monad_native ps gtest)
