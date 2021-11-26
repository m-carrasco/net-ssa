set(QUERY_SRC_DIRECTORY ${CMAKE_SOURCE_DIR}/souffle/src)
set(QUERY_INPUT_FILE ${QUERY_SRC_DIRECTORY}/ssa-query.dl)

set(SOUFFLE_BIN_DIR ${CMAKE_BINARY_DIR}/bin/souffle CACHE INTERNAL "")
set(QUERY_OUTPUT_EXECUTABLE ${SOUFFLE_BIN_DIR}/ssa-query CACHE INTERNAL "")

file(MAKE_DIRECTORY ${SOUFFLE_BIN_DIR})

add_custom_target(build-souffle
    COMMAND ${SOUFFLE_COMMAND} --generate=${SOUFFLE_BIN_DIR}/ssa-query.cpp --include-dir=${QUERY_SRC_DIRECTORY} ${QUERY_INPUT_FILE}
    COMMAND ${CLANGXX} -O3 -target x86_64-unknown-linux-gnu --std=c++17 -o ${QUERY_OUTPUT_EXECUTABLE} ${SOUFFLE_BIN_DIR}/ssa-query.cpp -static
    WORKING_DIRECTORY ${CMAKE_SOURCE_DIR}
    )
