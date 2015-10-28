add_library(pshost STATIC coreclrutil.cpp)
target_link_libraries(pshost dl icuuc)
target_compile_options(pshost PRIVATE -fPIC)

add_executable(powershell main.cpp)
target_link_libraries(powershell pshost)

install(TARGETS powershell pshost
  RUNTIME DESTINATION bin
  ARCHIVE DESTINATION lib)
