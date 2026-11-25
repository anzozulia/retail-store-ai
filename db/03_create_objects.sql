USE RetailStoreDb;
GO

CREATE OR ALTER FUNCTION dbo.fn_WeekStart (@Date DATE)
RETURNS DATE
WITH SCHEMABINDING
AS
BEGIN
    RETURN DATEADD(DAY, DATEDIFF(DAY, CAST('19000101' AS DATE), @Date) / 7 * 7, CAST('19000101' AS DATE));
END
GO

CREATE OR ALTER VIEW dbo.vw_WeeklySales
AS
SELECT si.ProductId,
       dbo.fn_WeekStart(CAST(s.SaleDate AS DATE)) AS WeekStart,
       SUM(si.Quantity) AS Quantity,
       SUM(si.Quantity * si.UnitPrice) AS Revenue
FROM dbo.SaleItems AS si
JOIN dbo.Sales AS s ON s.SaleId = si.SaleId
GROUP BY si.ProductId, dbo.fn_WeekStart(CAST(s.SaleDate AS DATE));
GO

CREATE OR ALTER TRIGGER dbo.trg_SaleItems_UpdateStock
ON dbo.SaleItems
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Movement TABLE (ProductId INT PRIMARY KEY, Quantity INT NOT NULL);

    INSERT INTO @Movement (ProductId, Quantity)
    SELECT ProductId, SUM(Quantity)
    FROM
    (
        SELECT ProductId, Quantity FROM inserted
        UNION ALL
        SELECT ProductId, -Quantity FROM deleted
    ) AS m
    GROUP BY ProductId;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Products AS p WITH (UPDLOCK)
        JOIN @Movement AS m ON m.ProductId = p.ProductId
        WHERE p.StockQuantity < m.Quantity
    )
        THROW 50001, N'Not enough stock for one or more products.', 1;

    UPDATE p
    SET p.StockQuantity = p.StockQuantity - m.Quantity
    FROM dbo.Products AS p
    JOIN @Movement AS m ON m.ProductId = p.ProductId
    WHERE m.Quantity <> 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_CalculateDemandForecast
    @Weeks INT = 4,
    @AsOfDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Weeks NOT BETWEEN 1 AND 12
        THROW 50002, N'The moving average window must be between 1 and 12 weeks.', 1;

    IF @AsOfDate IS NULL
        SELECT @AsOfDate = CAST(MAX(SaleDate) AS DATE) FROM dbo.Sales;

    IF @AsOfDate IS NULL
        RETURN;

    DECLARE @TargetWeek DATE = dbo.fn_WeekStart(DATEADD(DAY, 1, @AsOfDate));

    BEGIN TRANSACTION;

    DELETE FROM dbo.DemandForecasts;

    INSERT INTO dbo.DemandForecasts (ProductId, WeekStart, ForecastQty, RecommendedOrderQty)
    SELECT p.ProductId,
           w.WeekStart,
           f.ForecastQty,
           CASE WHEN w.WeekStart = @TargetWeek
                THEN GREATEST(0, CAST(CEILING(f.ForecastQty * (7 + sup.LeadTimeDays) / 7.0) AS INT) + p.MinStock - p.StockQuantity)
           END
    FROM dbo.Products AS p
    JOIN dbo.Suppliers AS sup ON sup.SupplierId = p.SupplierId
    CROSS JOIN (SELECT DATEADD(WEEK, -value, @TargetWeek) AS WeekStart FROM GENERATE_SERIES(0, 12)) AS w
    CROSS APPLY
    (
        SELECT CAST(ISNULL(SUM(si.Quantity), 0) * 1.0 / @Weeks AS DECIMAL(10,2)) AS ForecastQty
        FROM dbo.SaleItems AS si
        JOIN dbo.Sales AS s ON s.SaleId = si.SaleId
        WHERE si.ProductId = p.ProductId
          AND s.SaleDate >= DATEADD(WEEK, -@Weeks, w.WeekStart)
          AND s.SaleDate < w.WeekStart
    ) AS f;

    COMMIT TRANSACTION;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_RestockProduct
    @ProductId INT,
    @Quantity INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @Quantity IS NULL OR @Quantity <= 0
        THROW 50003, N'Quantity must be greater than zero.', 1;

    UPDATE dbo.Products
    SET StockQuantity = StockQuantity + @Quantity
    WHERE ProductId = @ProductId;

    IF @@ROWCOUNT = 0
        THROW 50004, N'Product not found.', 1;
END
GO
