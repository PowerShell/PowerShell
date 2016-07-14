add_subdirectory(googletest)

add_executable(psl-native-test
  test-getfileowner.cpp
  test-locale.cpp
  test-getuserfrompid.cpp
  test-getcurrentprocessid.cpp
  test-getusername.cpp
  test-getcomputername.cpp
  test-getlinkcount.cpp
  test-getfullyqualifiedname.cpp
  test-isdirectory.cpp
  test-isfile.cpp
  test-issymlink.cpp
  test-isexecutable.cpp
  test-createsymlink.cpp
  test-createhardlink.cpp
  main.cpp)

# manually include gtest headers
target_include_directories(psl-native-test PRIVATE ${gtest_SOURCE_DIR}/include)

target_link_libraries(psl-native-test psl-native gtest)

add_test(NAME psl-native-test
  COMMAND psl-native-test --gtest_output=xml:native-tests.xml)
