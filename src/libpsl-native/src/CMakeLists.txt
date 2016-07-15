add_library(psl-native SHARED
  getstat.cpp
  getlstat.cpp
  getpwuid.cpp
  getuserfrompid.cpp
  getfileowner.cpp
  getcurrentprocessorid.cpp
  getusername.cpp
  getcomputername.cpp
  getlinkcount.cpp
  getfullyqualifiedname.cpp
  isfile.cpp
  isdirectory.cpp
  issymlink.cpp
  isexecutable.cpp
  setdate.cpp
  createhardlink.cpp
  createsymlink.cpp
  followsymlink.cpp)

target_include_directories(psl-native PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
