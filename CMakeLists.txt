cmake_minimum_required (VERSION 3.21)
project (NET_SSA)

list(APPEND CMAKE_MODULE_PATH "${CMAKE_CURRENT_SOURCE_DIR}/cmake")

find_package(Dotnet REQUIRED)
find_package(Souffle REQUIRED)
find_package(Python3 REQUIRED COMPONENTS Interpreter)

add_subdirectory(souffle)

add_custom_target(dotnet 
    COMMAND ${DOTNET_COMMAND} build -o ${CMAKE_CURRENT_BINARY_DIR}/bin/net-ssa
    WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
    )

add_custom_target(test
    COMMAND SSA_QUERY_BIN=${QUERY_OUTPUT_EXECUTABLE} ${DOTNET_COMMAND} test --verbosity normal
    WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
    )