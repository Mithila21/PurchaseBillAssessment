using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;

namespace PurchaseBill.Api.Controllers
{
    public class LoginRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public AuthController(
            IHttpClientFactory clientFactory,
            IConfiguration configuration)
        {
            _httpClient = clientFactory.CreateClient();
            _configuration = configuration;
        }

        [HttpGet("csrf")]
        public IActionResult GetCsrfToken(
            [FromServices] IAntiforgery antiforgery)
        {
            var tokens = antiforgery.GetAndStoreTokens(HttpContext);

            Response.Cookies.Append(
                "XSRF-TOKEN",
                tokens.RequestToken!,
                new CookieOptions
                {
                    HttpOnly = false,
                    SameSite = SameSiteMode.Lax,
                    Secure = Request.IsHttps,
                    Path = "/"
                }
            );

            return NoContent();
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            return Ok(new
            {
                email = User.Identity?.Name
            });
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return Ok(new
            {
                message = "Logged out successfully."
            });
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new
            {
                message = "Successfully connected to ASP.NET Core API!"
            });
        }

        [ValidateAntiForgeryToken]
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    message = "Email and password are required."
                });
            }

            // Prepare request for Enhanzer API
            var apiRequest = new
            {
                API_Action = "GetLoginData",
                Device_Id = "D001",
                Sync_Time = "",
                Company_Code = request.Email,
                API_Body = new
                {
                    Username = request.Email,
                    Pw = request.Password
                }
            };

            try
            {
                string url =
                    "https://ez-staging-api.azurewebsites.net/api/External_Api/POS_Api/Invoke";

                string json = JsonSerializer.Serialize(apiRequest);

                using var content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"
                );

                using var response =
                    await _httpClient.PostAsync(url, content);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return Unauthorized(new
                    {
                        message = "Invalid email or password."
                    });
                }

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(502, new
                    {
                        message = "External authentication service returned an error."
                    });
                }

                string responseText =
                    await response.Content.ReadAsStringAsync();

                using var result = JsonDocument.Parse(responseText);
                var root = result.RootElement;

                // Check API status
                if (!root.TryGetProperty("Status_Code", out var status) ||
                    status.ValueKind != JsonValueKind.Number ||
                    !status.TryGetInt32(out int statusCode) ||
                    statusCode != 200)
                {
                    return Unauthorized(new
                    {
                        message = "Login was not approved."
                    });
                }

                // Read Response_Body
                if (!root.TryGetProperty("Response_Body", out var body) ||
                    body.ValueKind != JsonValueKind.Array ||
                    body.GetArrayLength() == 0)
                {
                    return Unauthorized(new
                    {
                        message = "No user data returned."
                    });
                }

                var user = body[0];

                if (user.ValueKind != JsonValueKind.Object ||
                    !user.TryGetProperty("User_Locations", out var locations) ||
                    locations.ValueKind != JsonValueKind.Array)
                {
                    return StatusCode(502, new
                    {
                        message = "User locations not found."
                    });
                }


                // Save locations first
                await SaveLocationsAsync(locations);

                // Create authenticated user identity
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, request.Email)
                };

                var identity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme
                );

                var principal = new ClaimsPrincipal(identity);

                // Create login session cookie
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal
                );

                return Ok(new
                {
                    message = "Login successful!",
                    locationCount = locations.GetArrayLength()
                });

            }
            catch (HttpRequestException)
            {
                return StatusCode(502, new
                {
                    message = "Could not reach the external API."
                });
            }
            catch (TaskCanceledException)
            {
                return StatusCode(504, new
                {
                    message = "External API request timed out."
                });
            }
            catch (JsonException)
            {
                return StatusCode(502, new
                {
                    message = "Unexpected external API response."
                });
            }
            catch (SqlException ex)
            {
                Console.Error.WriteLine(
                    "Database error: " + ex.Message
                );

                return StatusCode(500, new
                {
                    message = "Could not save locations to database."
                });
            }
        }

        // Save locations to Location_Details
        private async Task SaveLocationsAsync(JsonElement locations)
        {
            string connectionString =
                _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Database connection string is missing."
                );

            string password =
                _configuration["Database:Password"]
                ?? throw new InvalidOperationException(
                    "Database password is missing."
                );

            var connectionBuilder =
                new SqlConnectionStringBuilder(connectionString)
                {
                    Password = password
                };

            await using var connection =
                new SqlConnection(connectionBuilder.ConnectionString);

            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            foreach (var location in locations.EnumerateArray())
            {
                string code =
                    location.GetProperty("Location_Code").GetString() ?? "";

                string name =
                    location.GetProperty("Location_Name").GetString() ?? "";

                if (string.IsNullOrWhiteSpace(code) ||
                    string.IsNullOrWhiteSpace(name))
                {
                    throw new JsonException(
                        "Invalid location details received."
                    );
                }

                // Update existing locations or insert new ones
                string query = @"
                    UPDATE dbo.Location_Details
                    SET Location_Name = @Name
                    WHERE Location_Code = @Code;

                    IF @@ROWCOUNT = 0
                    BEGIN
                        INSERT INTO dbo.Location_Details
                            (Location_Code, Location_Name)
                        VALUES (@Code, @Name);
                    END;
                ";

                using var command =
                    new SqlCommand(query, connection, transaction);

                command.Parameters.Add(
                    "@Code", SqlDbType.NVarChar, 100
                ).Value = code;

                command.Parameters.Add(
                    "@Name", SqlDbType.NVarChar, 255
                ).Value = name;

                await command.ExecuteNonQueryAsync();
            }

            transaction.Commit();
        }
    }
}