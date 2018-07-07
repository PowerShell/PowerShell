cmake_minimum_required(VERSION 3.10.0)

project(TestService)

if (NOT $ENV{${WindowsSDKVersion}})
    message (FATAL_ERROR "WindowsSDKVersion environment variable not found")
endif ()

add_executable(TestService
  TestService.c)

link_libraries(ws2_32)

