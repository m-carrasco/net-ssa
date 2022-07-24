#!/bin/bash
set -e
apt-get install -y clang++-12 llvm-12 llvm-12-tools && ln -s /usr/bin/llvm-config-12 /usr/bin/llvm-config
