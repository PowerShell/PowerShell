add_library(psnative SHARED
  getcurrentprocessorid.cpp
  getusername.cpp
  getcomputername.cpp
  getlinkcount.cpp
  terminal.cpp)

target_link_libraries(psnative icuuc)
target_include_directories (psnative PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
