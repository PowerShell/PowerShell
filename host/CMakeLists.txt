add_library(pshost STATIC coreclrutil.cpp)
target_link_libraries(pshost dl icuuc)

add_executable(host_cmdline main.cpp)
target_link_libraries(host_cmdline pshost)
