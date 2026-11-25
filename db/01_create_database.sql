USE master;
GO

IF DB_ID(N'RetailStoreDb') IS NULL
    CREATE DATABASE RetailStoreDb
    ON PRIMARY
    (
        NAME = RetailStoreDb_data,
        FILENAME = '/var/opt/mssql/data/RetailStoreDb.mdf',
        SIZE = 64MB,
        FILEGROWTH = 64MB
    )
    LOG ON
    (
        NAME = RetailStoreDb_log,
        FILENAME = '/var/opt/mssql/data/RetailStoreDb_log.ldf',
        SIZE = 32MB,
        FILEGROWTH = 32MB
    );
GO

ALTER DATABASE RetailStoreDb SET COMPATIBILITY_LEVEL = 160;
ALTER DATABASE RetailStoreDb SET RECOVERY SIMPLE;
GO
