cmake_minimum_required(VERSION 3.21)
project(OwnPRNG)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_FLAGS "-no-pie -O2")

# this formatting library is needed for this project: https://github.com/fmtlib/fmt.git
set(INC_FMT $ENV{HOME}/libs/fmt-8.1.1)

add_library(StateMachineExe STATIC src/StateMachine.cpp)
target_include_directories(StateMachineExe PUBLIC include)

add_library(sha256 STATIC src/sha256.cpp)
target_include_directories(sha256 PUBLIC include)

add_library(RandomNumberDeviceExe STATIC src/RandomNumberDevice.cpp)
target_include_directories(RandomNumberDeviceExe PUBLIC include)
target_include_directories(RandomNumberDeviceExe PUBLIC ${INC_FMT}/include)
target_link_libraries(RandomNumberDeviceExe PRIVATE sha256)
target_link_libraries(RandomNumberDeviceExe PRIVATE StateMachineExe)
target_link_libraries(RandomNumberDeviceExe PRIVATE ${INC_FMT}/build/libfmt.a)

add_executable(PRNGinCPP src/main.cpp src/sha256.cpp)
target_include_directories(PRNGinCPP PUBLIC include)
target_include_directories(PRNGinCPP PUBLIC ${INC_FMT}/include)
target_link_libraries(PRNGinCPP PRIVATE ${INC_FMT}/build/libfmt.a)
target_link_libraries(PRNGinCPP PRIVATE RandomNumberDeviceExe)
