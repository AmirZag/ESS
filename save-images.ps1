$dest = "~\ESS_Publish\images"

New-Item -ItemType Directory -Force -Path $dest | Out-Null

docker compose build

Write-Host "Saving Docker images..."

docker save -o "$dest\essapi.tar" essapi
docker save -o "$dest\clientui.tar" ess.client
docker save -o "$dest\postgres.tar" postgres:17-alpine
docker save -o "$dest\pgadmin.tar" dpage/pgadmin4:9.8.0
docker save -o "$dest\dashboard.tar" mcr.microsoft.com/dotnet/aspire-dashboard:9.3
docker save -o "$dest\seq.tar" datalust/seq:2025.2
docker save -o "$dest\minio.tar" minio/minio:RELEASE.2025-09-07T16-13-09Z

Write-Host "All Docker images saved to $dest"
