#!/bin/bash
set -e

export SQLCMDPASSWORD="$SA_PASSWORD"

sql() {
    /opt/mssql-tools18/bin/sqlcmd -S "${DB_HOST:-localhost}" -U sa -C -b -I "$@"
}

echo "Waiting for SQL Server..."
for i in $(seq 1 60); do
    if sql -Q "SELECT 1" > /dev/null 2>&1; then
        break
    fi
    sleep 2
done

if [ "$1" = "--reset" ]; then
    echo "Dropping RetailStoreDb"
    sql -i /db/00_drop_database.sql
fi

EXISTS=$(sql -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name = N'RetailStoreDb'")

if [ "$EXISTS" = "0" ]; then
    for f in 01_create_database 02_create_tables 03_create_objects 04_seed_data; do
        echo "Running $f.sql"
        sql -i "/db/$f.sql"
    done
else
    echo "Running 03_create_objects.sql"
    sql -i /db/03_create_objects.sql
fi

echo "Running 05_security.sql"
sql -v AppPassword="$APP_PASSWORD" -i /db/05_security.sql
echo "Database is ready"
