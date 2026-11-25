USE RetailStoreDb;
GO

SET NOCOUNT ON;

INSERT INTO dbo.Categories (Name)
SELECT CONCAT(N'Category_', value)
FROM GENERATE_SERIES(1, 5);

INSERT INTO dbo.Suppliers (Name, Phone, Email, LeadTimeDays)
SELECT CONCAT(N'Supplier_', value),
       CONCAT(N'+38044100000', value),
       CONCAT(N'supplier', value, N'@example.com'),
       CHOOSE(value, 3, 5, 7, 10, 14)
FROM GENERATE_SERIES(1, 5);

INSERT INTO dbo.Products (Name, CategoryId, SupplierId, UnitPrice, StockQuantity, MinStock)
SELECT CONCAT(N'Product_', value),
       (value - 1) % 5 + 1,
       (value - 1) / 4 + 1,
       CAST(20 + value * 13 % 48 * 10 - 0.01 AS DECIMAL(10,2)),
       0,
       10 + value % 4 * 5
FROM GENERATE_SERIES(1, 20);

INSERT INTO dbo.Customers (Name, Phone, Email)
SELECT CONCAT(N'Customer_', value),
       CONCAT(N'+380670000', FORMAT(value, '000')),
       CONCAT(N'customer', value, N'@example.com')
FROM GENERATE_SERIES(1, 50);

DECLARE @Today DATE = CAST(GETDATE() AS DATE);
DECLARE @End DATE = DATEADD(DAY, -(DATEDIFF(DAY, CAST('19000101' AS DATE), @Today) % 7 + 1), @Today);
DECLARE @Start DATE = DATEADD(DAY, -181, @End);

SELECT d.value AS DayIndex,
       p.ProductId,
       CAST(ROUND(k.BaseDaily * t.TrendFactor * k.WeekdayFactor * k.Noise, 0) AS INT) AS Quantity
INTO #Demand
FROM GENERATE_SERIES(0, 181) AS d
CROSS JOIN dbo.Products AS p
CROSS APPLY
(
    SELECT 1.0 + p.ProductId * 7 % 20 / 4.0 AS BaseDaily,
           d.value / 181.0 AS Progress,
           CASE WHEN d.value % 7 >= 4 THEN 1.3 ELSE 1.0 END AS WeekdayFactor,
           0.5 + CAST(SUBSTRING(HASHBYTES('SHA2_256', CONCAT('noise-', d.value, '-', p.ProductId)), 1, 2) AS INT) % 1000 / 1000.0 AS Noise
) AS k
CROSS APPLY
(
    SELECT CASE p.ProductId % 4
               WHEN 0 THEN 1.0
               WHEN 1 THEN 0.5 + 1.0 * k.Progress
               WHEN 2 THEN 1.3 - 0.6 * k.Progress
               ELSE 1.0 + 0.4 * SIN(2 * PI() * d.value / 56.0)
           END AS TrendFactor
) AS t;

SELECT l.ProductId,
       l.Quantity,
       DATEADD(MINUTE, 540 + l.ReceiptNo * 18 + CAST(SUBSTRING(h.Hash, 1, 2) AS INT) % 18,
               CAST(DATEADD(DAY, l.DayIndex, @Start) AS DATETIME2(0))) AS SaleDate,
       CASE WHEN CAST(SUBSTRING(h.Hash, 3, 2) AS INT) % 10 < 3 THEN NULL
            ELSE 1 + CAST(SUBSTRING(h.Hash, 5, 2) AS INT) % 50
       END AS CustomerId
INTO #Lines
FROM
(
    SELECT m.DayIndex, m.ProductId, r.ReceiptNo, COUNT(*) AS Quantity
    FROM #Demand AS m
    CROSS APPLY GENERATE_SERIES(1, m.Quantity) AS u
    CROSS APPLY
    (
        SELECT CAST(SUBSTRING(HASHBYTES('SHA2_256', CONCAT('unit-', m.DayIndex, '-', m.ProductId, '-', u.value)), 1, 2) AS INT) % 40 AS ReceiptNo
    ) AS r
    WHERE m.Quantity > 0
    GROUP BY m.DayIndex, m.ProductId, r.ReceiptNo
) AS l
CROSS APPLY (SELECT HASHBYTES('SHA2_256', CONCAT('receipt-', l.DayIndex, '-', l.ReceiptNo)) AS Hash) AS h;

INSERT INTO dbo.Sales (SaleDate, CustomerId)
SELECT DISTINCT SaleDate, CustomerId
FROM #Lines
ORDER BY SaleDate;

UPDATE dbo.Products SET StockQuantity = 1000000;

INSERT INTO dbo.SaleItems (SaleId, ProductId, Quantity, UnitPrice)
SELECT s.SaleId, l.ProductId, l.Quantity, p.UnitPrice
FROM #Lines AS l
JOIN dbo.Sales AS s ON s.SaleDate = l.SaleDate
JOIN dbo.Products AS p ON p.ProductId = l.ProductId;

UPDATE p
SET p.StockQuantity = CASE WHEN p.ProductId % 5 = 3 THEN p.MinStock / 2
                           ELSE CAST(r.WeeklyAvg * (1.0 + p.ProductId % 4 * 0.6) AS INT) + p.MinStock
                      END
FROM dbo.Products AS p
CROSS APPLY
(
    SELECT ISNULL(SUM(w.Quantity), 0) / 4.0 AS WeeklyAvg
    FROM dbo.vw_WeeklySales AS w
    WHERE w.ProductId = p.ProductId
      AND w.WeekStart > DATEADD(WEEK, -4, @End)
) AS r;

DROP TABLE #Demand;
DROP TABLE #Lines;
GO

EXEC dbo.usp_CalculateDemandForecast;
GO
