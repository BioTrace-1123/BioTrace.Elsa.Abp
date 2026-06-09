#!/usr/bin/env bash
# 释放本地开发端口 / 等待 Host 就绪（供 VS Code preLaunchTask 使用）
set -euo pipefail

HOST_PORTS=(44388 44389)

port_to_hex() {
  printf '%04X' "$1"
}

find_pids_by_port() {
  local port=$1
  local hex_port
  hex_port=$(port_to_hex "$port")
  local hex_upper
  hex_upper=$(echo "$hex_port" | tr '[:lower:]' '[:upper:]')

  if [[ -r /proc/net/tcp ]]; then
    while read -r _ local_addr _ _ _ inode _; do
      local port_hex=${local_addr##*:}
      if [[ "${port_hex^^}" == "$hex_upper" && "$inode" != "0" ]]; then
        find_pids_by_socket_inode "$inode"
      fi
    done < <(tail -n +2 /proc/net/tcp 2>/dev/null || true)
  fi

  if [[ -r /proc/net/tcp6 ]]; then
    while read -r _ local_addr _ _ _ inode _; do
      local port_hex=${local_addr##*:}
      if [[ "${port_hex^^}" == "$hex_upper" && "$inode" != "0" ]]; then
        find_pids_by_socket_inode "$inode"
      fi
    done < <(tail -n +2 /proc/net/tcp6 2>/dev/null || true)
  fi
}

find_pids_by_socket_inode() {
  local inode=$1
  local pid
  for pid_dir in /proc/[0-9]*; do
    local pid=${pid_dir##*/}
    local fd
    for fd in "$pid_dir"/fd/*; do
      if [[ -L "$fd" ]] && readlink "$fd" 2>/dev/null | grep -q "socket:\[$inode\]"; then
        echo "$pid"
      fi
    done
  done
}

kill_pids() {
  local label=$1
  shift
  local -a pids=("$@")
  local -a unique_pids=()

  if ((${#pids[@]} == 0)); then
    return 0
  fi

  mapfile -t unique_pids < <(printf '%s\n' "${pids[@]}" | sort -u)
  echo "Stopping ${label}: ${unique_pids[*]}"
  kill -TERM "${unique_pids[@]}" 2>/dev/null || true
  sleep 0.5
  kill -KILL "${unique_pids[@]}" 2>/dev/null || true
}

free_port() {
  local port=$1
  local -a pids=()

  if command -v fuser >/dev/null 2>&1; then
    if fuser "${port}/tcp" >/dev/null 2>&1; then
      echo "Releasing port ${port} (fuser)..."
      fuser -k "${port}/tcp" >/dev/null 2>&1 || true
      sleep 0.5
      return 0
    fi
    return 0
  fi

  if command -v lsof >/dev/null 2>&1; then
    mapfile -t pids < <(lsof -ti:"${port}" 2>/dev/null || true)
    if ((${#pids[@]} > 0)); then
      kill_pids "processes on port ${port}" "${pids[@]}"
      return 0
    fi
    return 0
  fi

  mapfile -t pids < <(find_pids_by_port "$port" | sort -u)
  if ((${#pids[@]} > 0)); then
    kill_pids "processes on port ${port}" "${pids[@]}"
  fi
}

free_ports() {
  local ports=("$@")
  local port
  for port in "${ports[@]}"; do
    free_port "$port"
  done
}

free_host_processes() {
  local -a pids=()
  if command -v pgrep >/dev/null 2>&1; then
    mapfile -t pids < <(pgrep -f "BioTrace\.Elsa\.Abp\.HttpApi\.Host" 2>/dev/null || true)
    kill_pids "HttpApi.Host" "${pids[@]}"
  fi
  free_ports "${HOST_PORTS[@]}"
}

wait_for_host() {
  local url="https://localhost:44388/swagger/index.html"
  local max_attempts=180
  local attempt=0

  echo "Waiting for HttpApi.Host (${url})..."
  while ((attempt < max_attempts)); do
    if curl -kfsS -o /dev/null "$url" 2>/dev/null; then
      echo "HttpApi.Host is ready."
      return 0
    fi
    sleep 1
    ((attempt += 1))
  done

  echo "Timed out waiting for HttpApi.Host on https://localhost:44388." >&2
  return 1
}

case "${1:-}" in
  host|all)
    echo "Checking HttpApi.Host ports (${HOST_PORTS[*]})..."
    free_host_processes
    ;;
  wait-host)
    wait_for_host
    ;;
  *)
    echo "Usage: $0 {host|wait-host|all}" >&2
    exit 1
    ;;
esac
