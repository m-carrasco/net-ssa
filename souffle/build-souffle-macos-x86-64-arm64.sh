#!/bin/bash
set -e

cpp_file=$(mktemp -u).cpp
parent_path=$( cd "$(dirname "${BASH_SOURCE[0]}")" ; pwd -P )
include_dir=$parent_path/src
souffle_file=$include_dir/ssa-query.dl
target_os="macos-x86-64-arm64"
output_dir=$parent_path/bin/$target_os
output_bin=$output_dir/ssa-query-$target_os

echo "script path: "$parent_path
echo "soufle include dir:" $include_dir
echo "target souffle script:" $souffle_file
echo "temp cpp file:" $cpp_file
echo "output dir:" $output_dir
echo "output bin:" $output_bin
echo $(souffle --version)

souffle --generate=$cpp_file --include-dir=$include_dir $souffle_file
mkdir -p $output_dir
$CXX -O3 --std=c++17 -target x86_64-apple-macos -target arm64-apple-macos -o $output_bin $cpp_file
echo "Done"