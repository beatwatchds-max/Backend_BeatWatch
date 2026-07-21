#!/usr/bin/env bash
set -euo pipefail

compose_file="docker-compose.test.yml"

cleanup() {
  docker compose -f "$compose_file" down --volumes --remove-orphans
}

trap cleanup EXIT
docker compose -f "$compose_file" up --detach --wait
TEST_MONGODB_CONNECTION_STRING="mongodb://localhost:27017" \
  dotnet test "BeatWatch_BackEnd.Tests/BeatWatch_BackEnd.Tests.csproj" \
  --configuration Release \
  --no-restore
