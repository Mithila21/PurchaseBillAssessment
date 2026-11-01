using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace PurchaseBill.Api.Controllers
{
    // Each item sent from Angular
    public class PurchaseBillItemRequest
    {
        public string Item { get; set; } = "";
        public string LocationCode { get; set; } = "";
        public decimal StandardCost { get; set; }
        public decimal StandardPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Discount { get; set; }
    }

    // Complete bill sent from Angular
    public class SaveBillRequest
    {
        public List<PurchaseBillItemRequest> Items { get; set; } = new();
    }

    [ApiController]
    [Authorize]
    [Route("api/purchasebills")]
    public class PurchaseBillsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PurchaseBillsController> _logger;

        public PurchaseBillsController(
            IConfiguration configuration,
            ILogger<PurchaseBillsController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> SaveBill(
            [FromBody] SaveBillRequest request)
        {
            // Check that the bill contains items
            if (request?.Items == null || request.Items.Count == 0)
            {
                return BadRequest(new
                {
                    message = "Please add at least one item."
                });
            }

            // Allowed items from the assessment
            string[] allowedItems =
            {
                "Mango", "Apple", "Banana", "Orange",
                "Grapes", "Kiwi", "Strawberry"
            };

            // Validate all items
            foreach (var item in request.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Item) ||
                    !allowedItems.Contains(
                        item.Item.Trim(),
                        StringComparer.OrdinalIgnoreCase) ||
                    string.IsNullOrWhiteSpace(item.LocationCode) ||
                    item.LocationCode.Length > 100 ||
                    item.StandardCost < 0 ||
                    item.StandardPrice < 0 ||
                    item.Quantity <= 0 ||
                    item.Discount < 0 ||
                    item.Discount > 100)
                {
                    return BadRequest(new
                    {
                        message = "Invalid purchase bill item details."
                    });
                }
            }

            string connectionString =
                _configuration.GetConnectionString("DefaultConnection") ?? "";

            string password =
                _configuration["Database:Password"] ?? "";

            if (string.IsNullOrEmpty(connectionString) ||
                string.IsNullOrEmpty(password))
            {
                return StatusCode(500, new
                {
                    message = "Database configuration is missing."
                });
            }

            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                Password = password
            };

            // One ID groups all items belonging to this bill
            Guid billId = Guid.NewGuid();

            try
            {
                using var connection =
                    new SqlConnection(builder.ConnectionString);

                await connection.OpenAsync();

                using var transaction = connection.BeginTransaction();

                foreach (var item in request.Items)
                {
                    // Calculate totals again on the backend
                    decimal totalCost = Math.Round(
                        item.StandardCost * item.Quantity *
                        (1 - item.Discount / 100),
                        2
                    );

                    decimal totalSelling = Math.Round(
                        item.StandardPrice * item.Quantity,
                        2
                    );

                    string query = @"
                        INSERT INTO dbo.Purchase_Bill_Items
                        (
                            Bill_Id,
                            Item_Name,
                            Location_Code,
                            Standard_Cost,
                            Standard_Price,
                            Quantity,
                            Discount_Percent,
                            Total_Cost,
                            Total_Selling,
                            Created_By
                        )
                        VALUES
                        (
                            @BillId,
                            @ItemName,
                            @LocationCode,
                            @StandardCost,
                            @StandardPrice,
                            @Quantity,
                            @Discount,
                            @TotalCost,
                            @TotalSelling,
                            @CreatedBy
                        )";

                    using var command =
                        new SqlCommand(query, connection, transaction);

                    command.Parameters.AddWithValue("@BillId", billId);
                    command.Parameters.AddWithValue("@ItemName", item.Item.Trim());
                    command.Parameters.AddWithValue("@LocationCode", item.LocationCode);
                    command.Parameters.AddWithValue("@StandardCost", item.StandardCost);
                    command.Parameters.AddWithValue("@StandardPrice", item.StandardPrice);
                    command.Parameters.AddWithValue("@Quantity", item.Quantity);
                    command.Parameters.AddWithValue("@Discount", item.Discount);
                    command.Parameters.AddWithValue("@TotalCost", totalCost);
                    command.Parameters.AddWithValue("@TotalSelling", totalSelling);
                    command.Parameters.AddWithValue(
                        "@CreatedBy",
                        User.Identity?.Name ?? ""
                    );

                    await command.ExecuteNonQueryAsync();
                }

                // Save all items together
                transaction.Commit();

                return Ok(new
                {
                    message = "Purchase bill saved successfully!",
                    billId = billId,
                    totalItems = request.Items.Count,
                    totalQuantity = request.Items.Sum(x => x.Quantity)
                });
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                return BadRequest(new
                {
                    message = "Invalid batch or database constraint."
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Failed to save purchase bill.");

                return StatusCode(500, new
                {
                    message = "Could not save the purchase bill."
                });
            }
        }
    }
}
