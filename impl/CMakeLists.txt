add_library(ps SHARED
  getcurrentprocessorid.cpp
  getusername.cpp
  getcomputername.cpp
  terminal.cpp)

target_link_libraries(ps icuuc)
target_include_directories (ps PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
