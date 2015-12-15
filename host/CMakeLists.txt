add_library(pshost STATIC coreclrutil.cpp)
target_link_libraries(pshost dl ${Boost_LIBRARIES})
target_include_directories(pshost PRIVATE ${Boost_INCLUDE_DIRS})
target_compile_options(pshost PRIVATE -fPIC)

set(Boost_USE_STATIC_LIBS ON)
find_package(Boost 1.54.0 REQUIRED COMPONENTS filesystem system)

# simple native host executable test
add_executable(powershell main.cpp)
target_link_libraries(powershell pshost ${Boost_LIBRARIES})
