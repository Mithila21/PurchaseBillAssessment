
-- Enhanzer Purchase Bill Assessment
-- Database Setup Script

-- Create database
IF DB_ID('PurchaseBillDB') IS NULL
BEGIN
    CREATE DATABASE PurchaseBillDB;
END;
GO

USE PurchaseBillDB;
GO

-- Table 1: Locations from Enhanzer Login API
IF OBJECT_ID('dbo.Location_Details', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Location_Details
    (
        Location_Code NVARCHAR(100) NOT NULL PRIMARY KEY,
        Location_Name NVARCHAR(255) NOT NULL
    );
END;
GO

-- Table 2: Purchase Bill Items
IF OBJECT_ID('dbo.Purchase_Bill_Items', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Purchase_Bill_Items
    (
        ID INT IDENTITY(1,1) PRIMARY KEY,
        Bill_Id UNIQUEIDENTIFIER NOT NULL,
        Item_Name NVARCHAR(100) NOT NULL,
        Location_Code NVARCHAR(100) NOT NULL,
        Standard_Cost DECIMAL(18,2) NOT NULL,
        Standard_Price DECIMAL(18,2) NOT NULL,
        Quantity INT NOT NULL CHECK (Quantity > 0),
        Discount_Percent DECIMAL(5,2) NOT NULL
            CHECK (Discount_Percent BETWEEN 0 AND 100),
        Total_Cost DECIMAL(18,2) NOT NULL,
        Total_Selling DECIMAL(18,2) NOT NULL,
        Created_By NVARCHAR(320) NOT NULL,
        Created_At DATETIME2 NOT NULL
            DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_PurchaseBill_Location
        FOREIGN KEY (Location_Code)
        REFERENCES dbo.Location_Details(Location_Code)
    );
END;
GO

-- Verify tables
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
GO
