ARG BASE_IMAGE
FROM ${BASE_IMAGE}

ENV PATH="$PATH:/home/ubuntu/.dotnet/tools"

COPY --chown=ubuntu:mygroup . ${NET_SSA_SRC_DIR}

RUN ./ci/build.sh && \
    dotnet test --verbosity normal && \
    lit ./integration-test -vv
