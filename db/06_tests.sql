USE RetailStoreDb;
GO

SET NOCOUNT ON;

SELECT 'Categories' AS TableName, COUNT(*) AS RowsCount FROM dbo.Categories
UNION ALL SELECT 'Suppliers', COUNT(*) FROM dbo.Suppliers
UNION ALL SELECT 'Products', COUNT(*) FROM dbo.Products
UNION ALL SELECT 'Customers', COUNT(*) FROM dbo.Customers
UNION ALL SELECT 'Sales', COUNT(*) FROM dbo.Sales
UNION ALL SELECT 'SaleItems', COUNT(*) FROM dbo.SaleItems
UNION ALL SELECT 'DemandForecasts', COUNT(*) FROM dbo.DemandForecasts;
GO

SELECT OBJECT_NAME(parent_object_id) AS TableName, name AS ConstraintName, type_desc AS ConstraintType
FROM sys.objects
WHERE type IN ('PK', 'F', 'UQ', 'C', 'D')
ORDER BY TableName, ConstraintType, ConstraintName;

SELECT name AS TriggerName, is_disabled FROM sys.triggers;
GO

BEGIN TRANSACTION;

DECLARE @SaleId INT;
SELECT StockQuantity AS StockBefore FROM dbo.Products WHERE ProductId = 1;

INSERT INTO dbo.Sales (CustomerId) VALUES (1);
SET @SaleId = SCOPE_IDENTITY();
INSERT INTO dbo.SaleItems (SaleId, ProductId, Quantity, UnitPrice)
SELECT @SaleId, ProductId, 2, UnitPrice FROM dbo.Products WHERE ProductId = 1;
SELECT StockQuantity AS StockAfterSale FROM dbo.Products WHERE ProductId = 1;

UPDATE dbo.SaleItems SET Quantity = 5 WHERE SaleId = @SaleId;
SELECT StockQuantity AS StockAfterUpdate FROM dbo.Products WHERE ProductId = 1;

DELETE FROM dbo.Sales WHERE SaleId = @SaleId;
SELECT StockQuantity AS StockAfterDelete FROM dbo.Products WHERE ProductId = 1;

ROLLBACK TRANSACTION;
GO

BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @SaleId INT;
    INSERT INTO dbo.Sales (CustomerId) VALUES (NULL);
    SET @SaleId = SCOPE_IDENTITY();
    INSERT INTO dbo.SaleItems (SaleId, ProductId, Quantity, UnitPrice) VALUES (@SaleId, 3, 100000, 10);
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    SELECT ERROR_NUMBER() AS ErrorNumber, ERROR_MESSAGE() AS ErrorMessage;
END CATCH
GO

BEGIN TRY
    INSERT INTO dbo.Products (Name, CategoryId, SupplierId, UnitPrice, MinStock) VALUES (N'Bad_Product', 1, 1, -5, 10);
END TRY
BEGIN CATCH
    SELECT ERROR_NUMBER() AS ErrorNumber, ERROR_MESSAGE() AS ErrorMessage;
END CATCH
GO

EXEC dbo.usp_CalculateDemandForecast @Weeks = 4;
GO

SELECT p.Name AS Product, df.WeekStart, df.ForecastQty, s.LeadTimeDays, p.MinStock, p.StockQuantity, df.RecommendedOrderQty
FROM dbo.DemandForecasts AS df
JOIN dbo.Products AS p ON p.ProductId = df.ProductId
JOIN dbo.Suppliers AS s ON s.SupplierId = p.SupplierId
WHERE df.RecommendedOrderQty IS NOT NULL
ORDER BY df.RecommendedOrderQty DESC, p.ProductId;

SELECT df.WeekStart, df.ForecastQty, ws.Quantity AS ActualQty
FROM dbo.DemandForecasts AS df
LEFT JOIN dbo.vw_WeeklySales AS ws ON ws.ProductId = df.ProductId AND ws.WeekStart = df.WeekStart
WHERE df.ProductId = 17
ORDER BY df.WeekStart;

SELECT p.Name AS Product,
       CAST(AVG(ABS(df.ForecastQty - ws.Quantity)) AS DECIMAL(10,2)) AS MAE
FROM dbo.DemandForecasts AS df
JOIN dbo.vw_WeeklySales AS ws ON ws.ProductId = df.ProductId AND ws.WeekStart = df.WeekStart
JOIN dbo.Products AS p ON p.ProductId = df.ProductId
WHERE df.RecommendedOrderQty IS NULL
GROUP BY p.ProductId, p.Name
ORDER BY p.ProductId;
GO
