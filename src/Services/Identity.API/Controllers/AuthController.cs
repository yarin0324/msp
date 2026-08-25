using Common.Security;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Username and password are required." });
            }

            var jwtOptions = _configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

            // 範例身分驗證邏輯
            string userId;
            string email;
            var roles = new List<string>();

            if (request.Username.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                userId = "USR-ADMIN-001";
                email = "admin@msp.local";
                roles.Add("Admin");
                roles.Add("Customer");
            }
            else
            {
                userId = $"USR-{Math.Abs(request.Username.GetHashCode()) % 10000:D4}";
                email = $"{request.Username.ToLower()}@msp.local";
                roles.Add("Customer");
            }

            var token = JwtTokenGenerator.GenerateToken(
                userId: userId,
                userName: request.Username,
                email: email,
                roles: roles,
                options: jwtOptions
            );

            return Ok(new LoginResponse
            {
                Token = token,
                TokenType = "Bearer",
                ExpiresIn = jwtOptions.ExpirationMinutes * 60,
                UserId = userId,
                UserName = request.Username,
                Email = email,
                Roles = roles
            });
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
    }
}
