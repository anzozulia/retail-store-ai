USE RetailStoreDb;
GO

CREATE TABLE dbo.Categories
(
    CategoryId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL CONSTRAINT UQ_Categories_Name UNIQUE
);

CREATE TABLE dbo.Suppliers
(
    SupplierId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Suppliers PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL CONSTRAINT UQ_Suppliers_Name UNIQUE,
    Phone NVARCHAR(20) NULL,
    Email NVARCHAR(100) NULL,
    LeadTimeDays INT NOT NULL,
    CONSTRAINT CK_Suppliers_Email CHECK (Email LIKE N'%_@_%._%'),
    CONSTRAINT CK_Suppliers_LeadTimeDays CHECK (LeadTimeDays BETWEEN 1 AND 60)
);

CREATE TABLE dbo.Products
(
    ProductId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL CONSTRAINT UQ_Products_Name UNIQUE,
    CategoryId INT NOT NULL CONSTRAINT FK_Products_Categories REFERENCES dbo.Categories (CategoryId),
    SupplierId INT NOT NULL CONSTRAINT FK_Products_Suppliers REFERENCES dbo.Suppliers (SupplierId),
    UnitPrice DECIMAL(10,2) NOT NULL,
    StockQuantity INT NOT NULL CONSTRAINT DF_Products_StockQuantity DEFAULT 0,
    MinStock INT NOT NULL,
    CONSTRAINT CK_Products_UnitPrice CHECK (UnitPrice > 0),
    CONSTRAINT CK_Products_StockQuantity CHECK (StockQuantity >= 0),
    CONSTRAINT CK_Products_MinStock CHECK (MinStock >= 0)
);

CREATE TABLE dbo.Customers
(
    CustomerId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Phone NVARCHAR(20) NULL,
    Email NVARCHAR(100) NULL,
    CONSTRAINT CK_Customers_Email CHECK (Email LIKE N'%_@_%._%')
);

CREATE TABLE dbo.Sales
(
    SaleId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sales PRIMARY KEY,
    SaleDate DATETIME2(0) NOT NULL CONSTRAINT DF_Sales_SaleDate DEFAULT SYSDATETIME(),
    CustomerId INT NULL CONSTRAINT FK_Sales_Customers REFERENCES dbo.Customers (CustomerId) ON DELETE SET NULL
);

CREATE TABLE dbo.SaleItems
(
    SaleItemId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SaleItems PRIMARY KEY,
    SaleId INT NOT NULL CONSTRAINT FK_SaleItems_Sales REFERENCES dbo.Sales (SaleId) ON DELETE CASCADE,
    ProductId INT NOT NULL CONSTRAINT FK_SaleItems_Products REFERENCES dbo.Products (ProductId),
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(10,2) NOT NULL,
    CONSTRAINT UQ_SaleItems_Sale_Product UNIQUE (SaleId, ProductId),
    CONSTRAINT CK_SaleItems_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_SaleItems_UnitPrice CHECK (UnitPrice > 0)
);

CREATE TABLE dbo.DemandForecasts
(
    ForecastId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DemandForecasts PRIMARY KEY,
    ProductId INT NOT NULL CONSTRAINT FK_DemandForecasts_Products REFERENCES dbo.Products (ProductId) ON DELETE CASCADE,
    WeekStart DATE NOT NULL,
    ForecastQty DECIMAL(10,2) NOT NULL,
    RecommendedOrderQty INT NULL,
    CalculatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_DemandForecasts_CalculatedAt DEFAULT SYSDATETIME(),
    CONSTRAINT UQ_DemandForecasts_Product_Week UNIQUE (ProductId, WeekStart),
    CONSTRAINT CK_DemandForecasts_WeekStart CHECK (DATEDIFF(DAY, CAST('19000101' AS DATE), WeekStart) % 7 = 0),
    CONSTRAINT CK_DemandForecasts_ForecastQty CHECK (ForecastQty >= 0),
    CONSTRAINT CK_DemandForecasts_RecommendedOrderQty CHECK (RecommendedOrderQty >= 0)
);

CREATE INDEX IX_Products_CategoryId ON dbo.Products (CategoryId);
CREATE INDEX IX_Products_SupplierId ON dbo.Products (SupplierId);
CREATE INDEX IX_Sales_CustomerId ON dbo.Sales (CustomerId);
CREATE INDEX IX_Sales_SaleDate ON dbo.Sales (SaleDate);
CREATE INDEX IX_SaleItems_ProductId ON dbo.SaleItems (ProductId);
GO
