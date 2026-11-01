-- Create the database
IF DB_ID('PurchaseBillDB') IS NULL
    BEGIN
        CREATE DATABASE PurchaseBillDB;
    END


GO
-- Select our database
USE PurchaseBillDB;


GO
-- Create the Locations table
IF OBJECT_ID('dbo.Location_Details', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Location_Details (
            Location_Code NVARCHAR (100) PRIMARY KEY,
            Location_Name NVARCHAR (255) NOT NULL
        );
    END


GO
-- Check the table
SELECT *
FROM   dbo.Location_Details;