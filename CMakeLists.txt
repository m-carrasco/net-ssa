cmake_minimum_required (VERSION 3.21)
project (NET_SSA)

list(APPEND CMAKE_MODULE_PATH "${CMAKE_CURRENT_SOURCE_DIR}/cmake")

find_package(Dotnet REQUIRED)
find_package(Souffle REQUIRED)
find_package(Python3 REQUIRED COMPONENTS Interpreter)
find_package(Mono 6.12 REQUIRED COMPONENTS)
find_program(LLVM_CONFIG NAMES llvm-config REQUIRED HINTS /usr/bin/llvm-config)
message(STATUS "Using llvm-config: ${LLVM_CONFIG}")
exec_program("${LLVM_CONFIG} --bindir" OUTPUT_VARIABLE LLVM_BINDIR)

add_subdirectory(souffle)

set(NET_SSA_BIN_DIR ${CMAKE_CURRENT_BINARY_DIR}/bin/net-ssa)
add_custom_target(build-dotnet 
    COMMAND ${DOTNET_COMMAND} build -o ${NET_SSA_BIN_DIR}
    WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
    )

add_custom_target(check-unit-test
    COMMAND SSA_QUERY_BIN=${QUERY_OUTPUT_EXECUTABLE} ${DOTNET_COMMAND} test --verbosity normal
    WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
    )

configure_file("${CMAKE_CURRENT_SOURCE_DIR}/integration-test/lit.site.cfg.py.in" "${CMAKE_CURRENT_BINARY_DIR}/integration-test/lit.site.cfg.py" @ONLY)
