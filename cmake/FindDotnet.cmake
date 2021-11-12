# Code taken from:
# https://github.com/metacall/core/blob/c09235893e972b99d9319cb84e8eb60fd63e33ac/cmake/FindDotNET.cmake

if(DOTNET_FOUND)
	set(DOTNET_FIND_QUIETLY TRUE)
endif()

# Define dotnet command
set(DOTNET_COMMAND dotnet CACHE FILEPATH "Path of 'dotnet' command")

# Detect dotnet command
execute_process(COMMAND ${DOTNET_COMMAND}
	RESULT_VARIABLE DOTNET_COMMAND_RESULT
	OUTPUT_QUIET
)

# Set found variable (TODO: Review 129 state in Debian)
if(DOTNET_COMMAND_RESULT EQUAL 0 OR DOTNET_COMMAND_RESULT EQUAL 129)
	set(DOTNET_FOUND TRUE)
else()
	set(DOTNET_FOUND FALSE)
endif()

# Detect dotnet variables
if(DOTNET_FOUND)
	# Detect dotnet version
	execute_process(COMMAND ${DOTNET_COMMAND} --version
		RESULT_VARIABLE DOTNET_COMMAND_RESULT
		OUTPUT_VARIABLE DOTNET_COMMAND_OUTPUT
		OUTPUT_STRIP_TRAILING_WHITESPACE
	)
	if(DOTNET_COMMAND_RESULT EQUAL 0)
		set(DOTNET_VERSION "${DOTNET_COMMAND_OUTPUT}")
	endif()
endif()