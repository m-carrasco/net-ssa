ARG REPO=mcr.microsoft.com/dotnet/sdk
FROM $REPO:6.0-focal

COPY . /net-ssa

RUN apt update && \
    # Installing Souffle is not strictly required but it is handy in case
    # a new analysis is implemented. This environment should be enough for quick prototyping.
    chmod +x /net-ssa/ci/install-souffle.sh && /net-ssa/ci/install-souffle.sh && \
    chmod +x /net-ssa/ci/install-cmake.sh && /net-ssa/ci/install-cmake.sh && \
    chmod +x /net-ssa/ci/install-lit.sh && /net-ssa/ci/install-lit.sh && \
    chmod +x /net-ssa/ci/install-llvm.sh && /net-ssa/ci/install-llvm.sh && \
    chmod +x /net-ssa/ci/install-mono.sh && /net-ssa/ci/install-mono.sh

RUN cd /net-ssa && \
    dotnet build && \
    dotnet test --verbosity normal && \
    mkdir /build && \
    cd /build && \
    cmake ../net-ssa && \
    lit ./integration-test -vv
