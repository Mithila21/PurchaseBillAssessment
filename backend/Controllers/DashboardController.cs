
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace PurchaseBill.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public DashboardController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetDbConnection()
        {
            string connectionString =
                _configuration.GetConnectionString("DefaultConnection") ?? "";

            string password =
                _configuration["Database:Password"] ?? "";

            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                Password = password
            };

            return builder.ConnectionString;
        }

        // Widget 1: Latest 5 purchase orders
        [HttpGet("latest-orders")]
        public async Task<IActionResult> GetLatestOrders()
        {
            var orders = new List<object>();

            using var connection = new SqlConnection(GetDbConnection());
            await connection.OpenAsync();

            string sql = @"
                SELECT TOP 5
                    Bill_Id,
                    SUM(Total_Cost) AS NetAmount,
                    COUNT(*) AS NumberOfItems
                FROM dbo.Purchase_Bill_Items
                GROUP BY Bill_Id
                ORDER BY MAX(Created_At) DESC, MAX(ID) DESC";

            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                orders.Add(new
                {
                    purchaseOrderId = reader.GetGuid(0),
                    netAmount = reader.GetDecimal(1),
                    numberOfItems = reader.GetInt32(2)
                });
            }

            return Ok(orders);
        }

        // Widget 2: Oldest 10 purchase order items
        [HttpGet("oldest-items")]
        public async Task<IActionResult> GetOldestItems()
        {
            var items = new List<object>();

            using var connection = new SqlConnection(GetDbConnection());
            await connection.OpenAsync();

            string sql = @"
                SELECT TOP 10
                    Bill_Id,
                    Item_Name,
                    Quantity
                FROM dbo.Purchase_Bill_Items
                ORDER BY Created_At ASC, ID ASC";

            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                items.Add(new
                {
                    purchaseOrderId = reader.GetGuid(0),
                    itemName = reader.GetString(1),
                    quantity = reader.GetInt32(2)
                });
            }

            return Ok(items);
        }

        // Widget 3: Items grouped by name and quantity
        [HttpGet("item-summary")]
        public async Task<IActionResult> GetItemSummary()
        {
            var summary = new List<object>();

            using var connection = new SqlConnection(GetDbConnection());
            await connection.OpenAsync();

            string sql = @"
                SELECT
                    Item_Name,
                    SUM(Quantity) AS TotalQuantity
                FROM dbo.Purchase_Bill_Items
                GROUP BY Item_Name
                ORDER BY TotalQuantity DESC";

            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                summary.Add(new
                {
                    itemName = reader.GetString(0),
                    totalQuantity = reader.GetInt32(1)
                });
            }

            return Ok(summary);
        }
    }
}
