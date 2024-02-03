#!/bin/bash
set -e

CURRENT_DIR=$(dirname "$(readlink -f "$0")")

pushd $CURRENT_DIR/..

BASE_IMAGE=net-ssa-base:latest
docker build --file $CURRENT_DIR/docker/Dockerfile.base --build-arg USER_ID=$(id -u) --build-arg GROUP_ID=$(id -g) -t $BASE_IMAGE .
docker build --file $CURRENT_DIR/docker/Dockerfile --build-arg BASE_IMAGE=$BASE_IMAGE -t net-ssa:latest .

popd