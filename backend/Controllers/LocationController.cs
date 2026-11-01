using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;

namespace PurchaseBill.Api.Controllers
{
    public record LocationDto(
        string LocationCode,
        string LocationName
    );

    [ApiController]
    [Authorize]
    [Route("api/locations")]
    public class LocationsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public LocationsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        

        [HttpGet]
        public async Task<IActionResult> GetLocations()
        {
            var connectionString =
                _configuration.GetConnectionString("DefaultConnection");

            var password = _configuration["Database:Password"];

            if (string.IsNullOrEmpty(connectionString) ||
                string.IsNullOrEmpty(password))
            {
                return StatusCode(500, new
                {
                    message = "Database configuration is missing."
                });
            }

            var builder =
                new SqlConnectionStringBuilder(connectionString)
                {
                    Password = password
                };

            var locations = new List<LocationDto>();

            try
            {
                using var connection =
                    new SqlConnection(builder.ConnectionString);

                await connection.OpenAsync();

                string query = @"
                    SELECT Location_Code, Location_Name
                    FROM dbo.Location_Details
                    ORDER BY Location_Name";

                using var command =
                    new SqlCommand(query, connection);

                using var reader =
                    await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    locations.Add(new LocationDto(
                        reader.GetString(0),
                        reader.GetString(1)
                    ));
                }

                return Ok(locations);
            }
            catch (SqlException)
            {
                return StatusCode(500, new
                {
                    message = "Could not retrieve locations."
                });
            }
        }
    }
}
