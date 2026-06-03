FROM mcr.microsoft.com/dotnet/sdk:10.0

ARG USERNAME=vscode
ARG USER_UID=1000
ARG USER_GID=$USER_UID

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        git \
        curl \
        ca-certificates \
        postgresql-client \
    && rm -rf /var/lib/apt/lists/*

RUN existing_group="$(getent group "${USER_GID}" | cut -d: -f1)" \
    && if [ "${existing_group}" != "${USERNAME}" ]; then \
        groupmod -n "${USERNAME}" "${existing_group}"; \
    fi \
    && existing_user="$(getent passwd "${USER_UID}" | cut -d: -f1)" \
    && if [ "${existing_user}" != "${USERNAME}" ]; then \
        usermod -l "${USERNAME}" -d "/home/${USERNAME}" -m "${existing_user}"; \
    fi \
    && mkdir -p /workspaces \
    && chown -R "${USERNAME}:${USERNAME}" /workspaces

USER $USERNAME
WORKDIR /workspaces/BioTrace.Elsa.Abp
