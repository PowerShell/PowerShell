include(CheckCXXSourceCompiles)
check_cxx_source_compiles(
    "
    #include <string.h>
    int main()
    {
        char buffer[1];
        char* c = strerror_r(0, buffer, 0);
        (void)c;
    }
    "
    HAVE_GNU_STRERROR_R)

configure_file(${CMAKE_CURRENT_SOURCE_DIR}/config.h.in ${CMAKE_CURRENT_BINARY_DIR}/config.h)

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
  getstrerror.cpp
  isfile.cpp
  isdirectory.cpp
  issymlink.cpp
  isexecutable.cpp
  setdate.cpp
  createhardlink.cpp
  createsymlink.cpp
  followsymlink.cpp)

target_include_directories(psl-native PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
