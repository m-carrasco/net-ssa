#!/bin/bash
set -e

CURRENT_DIR=$(dirname "$(readlink -f "$0")")

pushd $CURRENT_DIR/..
docker run --rm -it -v $(pwd):/home/ubuntu/net-ssa net-ssa:latest
popd