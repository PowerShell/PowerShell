add_library(pshost STATIC coreclrutil.cpp)
target_link_libraries(pshost dl icuuc)

add_executable(powershell main.cpp)
target_link_libraries(powershell pshost)
