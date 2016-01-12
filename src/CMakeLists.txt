add_library(psl-native SHARED
  getcurrentprocessorid.cpp
  getusername.cpp
  getcomputername.cpp
  getlinkcount.cpp
  getfullyqualifiedname.cpp
  issymlink.cpp
  isexecutable.cpp
  setdate.cpp
  terminal.cpp)

target_include_directories(psl-native PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
