#!/bin/bash
# This might be useful to determine a more accurate list of dependencies
# https://github.com/souffle-lang/souffle/blob/master/docker/ubuntu/focal-base/Dockerfile
curl -LJO https://github.com/souffle-lang/souffle/releases/download/2.0.2/souffle_2.0.2-1_amd64.deb
apt-get update && apt-get install -y libffi-dev mcpp
curl -LJO http://mirrors.kernel.org/ubuntu/pool/main/libf/libffi/libffi6_3.2.1-8_amd64.deb
dpkg -i libffi6_3.2.1-8_amd64.deb
dpkg -i souffle_2.0.2-1_amd64.deb; apt-get -y -f install
