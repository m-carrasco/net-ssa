#!/bin/bash
set -e

CURRENT_DIR=$(dirname "$(readlink -f "$0")")

pushd $CURRENT_DIR/..

rm -rf net-ssa-cli/obj net-ssa-lib/obj unit-test/obj net-ssa-cli/bin net-ssa-lib/bin unit-test/bin

dotnet clean
rm -rf /tmp/build

dotnet build
./ci/nuget-pack.sh "0.0.0" "0.0.0" "0.0.0" "0.0.0" "/tmp/build/bin/net-ssa/package"

popd