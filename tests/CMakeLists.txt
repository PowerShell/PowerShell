add_subdirectory(googletest)

add_executable(psnative-test
  test-getcurrentprocessid.cpp
  test-getusername.cpp
  test-getcomputername.cpp
  test-getlinkcount.cpp
  main.cpp)

# manually include gtest headers
target_include_directories(psnative-test PRIVATE ${gtest_SOURCE_DIR}/include)

target_link_libraries(psnative-test psnative gtest)

add_test(NAME psnative-test
  COMMAND psnative-test --gtest_output=xml:${MONAD_LINUX_SOURCE_DIR}/native-tests.xml)
