add_library(psl-native SHARED
  getstat.cpp
  getpwuid.cpp
  getppid.cpp
  getuserfrompid.cpp
  getfileowner.cpp
  getcurrentthreadid.cpp
  getcurrentprocessorid.cpp
  getusername.cpp
  getcomputername.cpp
  getlinkcount.cpp
  getfullyqualifiedname.cpp
  geterrorcategory.cpp
  isfile.cpp
  isdirectory.cpp
  issymlink.cpp
  isexecutable.cpp
  setdate.cpp
  createhardlink.cpp
  createsymlink.cpp
  followsymlink.cpp)

target_include_directories(psl-native PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
