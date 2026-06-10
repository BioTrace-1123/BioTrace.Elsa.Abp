FROM mcr.microsoft.com/dotnet/sdk:10.0

ARG USERNAME=vscode
ARG USER_UID=1000
ARG USER_GID=$USER_UID

# Rename the base image's UID/GID 1000 user (ubuntu) to vscode, or create vscode when missing.
RUN if ! getent group "${USERNAME}" >/dev/null; then \
        if getent group "${USER_GID}" >/dev/null; then \
            existing_group="$(getent group "${USER_GID}" | cut -d: -f1)"; \
            groupmod -n "${USERNAME}" "${existing_group}"; \
        else \
            groupadd --gid "${USER_GID}" "${USERNAME}"; \
        fi; \
    fi \
    && if ! id -u "${USERNAME}" >/dev/null 2>&1; then \
        if getent passwd "${USER_UID}" >/dev/null; then \
            existing_user="$(getent passwd "${USER_UID}" | cut -d: -f1)"; \
            usermod -l "${USERNAME}" -d "/home/${USERNAME}" -m "${existing_user}"; \
        else \
            useradd --uid "${USER_UID}" --gid "${USER_GID}" -m "${USERNAME}"; \
        fi; \
    fi

# Docker Desktop / daemon proxy can break apt during image build.
RUN unset http_proxy https_proxy HTTP_PROXY HTTPS_PROXY \
    && apt-get update \
    && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends postgresql-client openssh-client nodejs npm \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /workspaces \
    && chown -R "${USERNAME}:${USERNAME}" /workspaces

# Chromium system libraries for Playwright MCP and E2E tests.
RUN unset http_proxy https_proxy HTTP_PROXY HTTPS_PROXY \
    && apt-get update \
    && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
        libglib2.0-0 libnss3 libnspr4 libatk1.0-0 libatk-bridge2.0-0 libcups2 libdrm2 \
        libxkbcommon0 libxcomposite1 libxdamage1 libxfixes3 libxrandr2 libgbm1 \
        libpango-1.0-0 libcairo2 libasound2 libx11-6 libxcb1 libxext6 libxi6 \
    && rm -rf /var/lib/apt/lists/* \
    && npm install -g playwright@latest \
    && npx playwright install-deps chromium \
    && npm uninstall -g playwright

USER $USERNAME
RUN dotnet tool install -g git-credential-manager --version 2.8.0
ENV PATH="/home/vscode/.dotnet/tools:${PATH}"

WORKDIR /workspaces/BioTrace.Elsa.Abp
