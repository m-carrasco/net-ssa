#!/bin/bash
set -e

CURRENT_DIR=$(dirname "$(readlink -f "$0")")

VERSION=$1
ASSEMBLY_VERSION=$2
INFORMATIONAL_VERSION=$3
PACKAGE_VERSION=$4
OUTPUT_DIR=$5

pushd $CURRENT_DIR/..

dotnet pack net-ssa.sln -o:$OUTPUT_DIR --include-symbols --include-source /p:Version=$VERSION /p:AssemblyVersion=$ASSEMBLY_VERSION /p:InformationalVersion=$INFORMATIONAL_VERSION /p:PackageVersion=$PACKAGE_VERSION

popd