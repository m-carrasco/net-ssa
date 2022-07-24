#!/bin/bash
set -e
curl -s "https://cmake.org/files/v3.21/cmake-3.21.1-linux-x86_64.tar.gz" | tar --strip-components=1 -xz -C /usr/local