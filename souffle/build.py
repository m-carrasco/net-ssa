#!/usr/bin/env python3

# This script requires 'souffle' to be in path.
# 'souffle' is called in order to build the executables 
# from our datalog queries, which are stored in '__file__/bins'
# The script expects to find the 'src' folder at the same
# folder where this python script is stored.

import os
import subprocess

OUTPUT_DIRECTORY_NAME = "bins"
OUTPUT_EXECUTABLE_NAME = "ssa-query"
INPUT_QUERY_FILE = "ssa-query.dl"
INPUT_SRC_DIRECTORY_NAME = "src"

def main():
    this_script_directory = parent_dir(os.path.realpath(__file__))
    include_directory = os.path.join(this_script_directory, INPUT_SRC_DIRECTORY_NAME)
    input_query = os.path.join(include_directory, INPUT_QUERY_FILE)
    output_directory = os.path.join(this_script_directory, OUTPUT_DIRECTORY_NAME)
    output_exectuable = os.path.join(output_directory, OUTPUT_EXECUTABLE_NAME)
    create_dir(output_directory)
    command = " ".join(["souffle", "--dl-program="+output_exectuable, "--include-dir="+include_directory, input_query])
    print("Executing: " + command)
    subprocess.run(command, shell=True, check=True)

def create_dir(directory):
    if not os.path.exists(directory):
        os.makedirs(directory)

def parent_dir(path):
    return os.path.abspath(os.path.join(path, os.pardir))

if __name__ == "__main__":
    main()




