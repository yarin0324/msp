#!/bin/bash
# Wait for SQL Server to be ready and run initialization script
echo "Waiting for SQL Server to start..."
for i in {1..60}; do
    /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "SELECT 1" > /dev/null 2>&1
    if [ $? -eq 0 ]; then
        echo "SQL Server is ready. Running init.sql..."
        /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -i /docker-entrypoint-initdb.d/init.sql
        echo "Database initialization completed successfully."
        break
    fi
    echo "SQL Server not ready yet, waiting 2 seconds..."
    sleep 2
done
