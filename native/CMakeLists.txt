add_library(psnative SHARED
  getcurrentprocessorid.cpp
  getusername.cpp
  getcomputername.cpp
  getlinkcount.cpp
  getfullyqualifiedname.cpp
  issymlink.cpp
  isexecutable.cpp
  setdate.cpp
  terminal.cpp)

target_link_libraries(psnative icuuc)
target_include_directories (psnative PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})

install(TARGETS psnative
  LIBRARY DESTINATION lib)
