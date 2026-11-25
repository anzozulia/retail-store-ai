USE master;
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'retail_app')
    CREATE LOGIN retail_app WITH PASSWORD = N'$(AppPassword)', CHECK_POLICY = ON, DEFAULT_DATABASE = RetailStoreDb;
ELSE
    ALTER LOGIN retail_app WITH PASSWORD = N'$(AppPassword)';
GO

USE RetailStoreDb;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'retail_app')
    CREATE USER retail_app FOR LOGIN retail_app;
GO

ALTER ROLE db_datareader ADD MEMBER retail_app;
ALTER ROLE db_datawriter ADD MEMBER retail_app;
GRANT EXECUTE ON SCHEMA::dbo TO retail_app;
DENY ALTER ON SCHEMA::dbo TO retail_app;
GO
