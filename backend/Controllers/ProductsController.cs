
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public ProductsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            Password = _configuration["Database:Password"]
        };

        var products = new List<object>();

        using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        string sql = "SELECT ProductId, ProductName, Price, Quantity, Category FROM Products";

        using var command = new SqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            products.Add(new
            {
                id = reader.GetInt32(0),
                name = reader.GetString(1),
                price = reader.GetDecimal(2),
                quantity = reader.GetInt32(3),
                category = reader.GetString(4)
            });
        }

        return Ok(products);
    }

    //Example Practice API to add a product to the database
    [HttpPost]
    public async Task<IActionResult> AddProduct(
    [FromBody] ProductRequest product)
    {
        // 1. Validate the received data
        if (string.IsNullOrWhiteSpace(product.ProductName) ||
            product.ProductName.Length > 100 ||
            product.Price <= 0 ||
            product.Quantity <= 0)
        {
            return BadRequest("Invalid product details");
        }

        // 2. Connect to the database
        var connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            Password = _configuration["Database:Password"]
        };

        using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        // 3. Prepare the INSERT query
        string sql = @"INSERT INTO Products
                   (ProductName, Price, Quantity, Category)
                   OUTPUT INSERTED.ProductId
                   VALUES (@Name, @Price, @Quantity, @Category)";

        using var command = new SqlCommand(sql, connection);

        // 4. Provide the values safely
        command.Parameters.Add("@Name", SqlDbType.NVarChar, 100)
            .Value = product.ProductName;

        command.Parameters.Add("@Category", SqlDbType.NVarChar, 50)
            .Value = product.Category;

        var priceParameter = command.Parameters.Add(
            "@Price", SqlDbType.Decimal);

        priceParameter.Precision = 18;
        priceParameter.Scale = 2;
        priceParameter.Value = product.Price;

        command.Parameters.Add("@Quantity", SqlDbType.Int)
            .Value = product.Quantity;

        // 5. Execute the query
        int newId = (int)(await command.ExecuteScalarAsync())!;

        // 6. Return a success response
        return Ok(new
        {
            message = "Product saved successfully",
            productId = newId
        });
    }
}

public class ProductRequest
{
    public string ProductName { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }

}
