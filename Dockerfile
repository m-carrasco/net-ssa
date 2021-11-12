ARG REPO=mcr.microsoft.com/dotnet/sdk
FROM $REPO:6.0-focal

COPY . /net-ssa

RUN chmod +x /net-ssa/ci/install-souffle.sh && /net-ssa/ci/install-souffle.sh && \
    chmod +x /net-ssa/ci/install-cmake.sh && /net-ssa/ci/install-cmake.sh && \
    chmod +x /net-ssa/ci/install-lit.sh && /net-ssa/ci/install-lit.sh && \
    chmod +x /net-ssa/ci/install-llvm.sh && /net-ssa/ci/install-llvm.sh && \
    chmod +x /net-ssa/ci/install-mono.sh && /net-ssa/ci/install-mono.sh

RUN cd net-ssa && \
    rm -r -f -- build && \
    mkdir build && \
    cd build && \
    cmake .. && \
    make build-souffle && \
    make build-dotnet && \
    make check-unit-test && \
    lit ./integration-test -vv