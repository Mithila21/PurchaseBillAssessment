USE PurchaseBillDB;


GO
SELECT   TOP (20) Bill_Id,
                  Item_Name,
                  Location_Code,
                  Quantity,
                  Total_Cost,
                  Total_Selling,
                  Created_At
FROM     dbo.Purchase_Bill_Items
ORDER BY ID DESC;