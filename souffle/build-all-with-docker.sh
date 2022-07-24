#!/bin/bash
set -e

parent_path=$( cd "$(dirname "${BASH_SOURCE[0]}")" ; pwd -P )

echo "script path: "$parent_path

docker build -t net-ssa-souffle/net-ssa-souffle -f $parent_path/../Dockerfile-souffle $parent_path/../
container_id=$(docker create net-ssa-souffle/net-ssa-souffle)
docker cp "$container_id:/net-ssa/souffle/bin" "$parent_path"
docker rm "$container_id"

echo "Done"