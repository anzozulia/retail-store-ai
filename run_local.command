#!/bin/bash
cd "$(dirname "$0")"
export PATH="$PATH:/usr/local/bin:/opt/homebrew/bin:$HOME/.docker/bin:/usr/local/share/dotnet:$HOME/.dotnet/tools"

if [ ! -f .env ]; then
    echo "SA_PASSWORD=Sa$(openssl rand -hex 12)1" > .env
    echo "APP_PASSWORD=App$(openssl rand -hex 12)2" >> .env
    echo "Created .env with new local passwords"
fi
set -a
source .env
set +a

NAME=retailstore-mssql
if ! docker ps --format '{{.Names}}' | grep -q "^$NAME$"; then
    if docker ps -a --format '{{.Names}}' | grep -q "^$NAME$"; then
        docker start $NAME > /dev/null
    else
        docker run --platform linux/amd64 -d --name $NAME -p 14330:1433 \
            -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD="$SA_PASSWORD" \
            -e MSSQL_MEMORY_LIMIT_MB=2048 -e TZ=Europe/Kyiv \
            -v "$PWD/db:/db:ro" -v retailstore-mssql-data:/var/opt/mssql \
            mcr.microsoft.com/mssql/server:2022-latest > /dev/null
    fi
fi

docker exec -e SA_PASSWORD -e APP_PASSWORD $NAME bash /db/init-db.sh "$@" || exit 1

export ConnectionStrings__RetailStore="Server=localhost,14330;Database=RetailStoreDb;User Id=retail_app;Password=$APP_PASSWORD;TrustServerCertificate=True"
(sleep 8 && open http://localhost:5080) &
dotnet run --project src/RetailStore.Web --urls http://localhost:5080
