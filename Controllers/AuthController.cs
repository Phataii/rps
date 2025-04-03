
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using rps.Data;
using rps.Models;
using Microsoft.AspNetCore.Session;
using Microsoft.Extensions.Caching.Memory;

namespace rps.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        public AuthController(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        [HttpPost("add-user")]
        public async Task<IActionResult> CreateUser([FromBody] UserCreateDto userDto)
        {
            // Validate the incoming data
            if (userDto == null || string.IsNullOrEmpty(userDto.Name) || string.IsNullOrEmpty(userDto.Email))
            {
                return BadRequest("Name and Email are required.");
            }

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == userDto.Email))
            {
                return Conflict("Email is already in use.");
            }

            // Map UserCreateDto to User model
            var user = new User
            {
                Name = userDto.Name,
                Email = userDto.Email,
                DepartmentId = userDto.DepartmentId,
                DepartmentName = userDto.Department, // Get the name from the cache/db
                Faculty = userDto.FacultyId,
                IsActive = true,
            };

            // Add the user to the database
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(user);
        }


        [HttpPost("add-user-to-role")]
        public async Task<IActionResult> AddUserToRole([FromBody] UserRoleDto userRole)
        {
            
            var UR = new UserRole
            {
                RoleId = userRole.role,
                UserId = userRole.user
            };

            _context.UserRoles.Add(UR);
            await _context.SaveChangesAsync();
            return Ok(UR);
        }
        
        [HttpPost("edit-user-role")]
        public async Task<IActionResult> UpdateRole([FromBody] EditUserRoleDto model)
        {
            if (model == null)
            {
                return BadRequest("Invalid data.");
            }

            var userRole = await _context.UserRoles.FirstOrDefaultAsync(ur => ur.Id == model.Id);
            if (userRole == null)
            {
                return NotFound("User role not found.");
            }

            userRole.RoleId = model.Role;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Role updated successfully." });
        }

        [HttpGet("sso")]
        public IActionResult LoginWithGoogle()
        {
            // var clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
            var clientId = "472518449698-aa0f036e7mgpfce18ltj1m2hothmphtf.apps.googleusercontent.com";
            var redirectUri = "https://localhost:7011/api/auth/signin-google";
            var state = Guid.NewGuid().ToString(); // Optional for CSRF protection
            var scope = "openid email profile";

            var googleLoginUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                $"client_id={clientId}" +
                $"&redirect_uri={redirectUri}" +
                $"&response_type=code" +
                $"&scope={scope}";

            return Redirect(googleLoginUrl);
        }
 
        [HttpGet("signin-google")]
        public async Task<IActionResult> Google(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest("Authorization code not provided.");
            }

                var clientId = "472518449698-aa0f036e7mgpfce18ltj1m2hothmphtf.apps.googleusercontent.com";
                    var clientSecret = "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";
                    var redirectUri = "https://localhost:7011/api/auth/signin-google";

            using (var httpClient = new HttpClient())
            {
                // Step 1: Exchange authorization code for tokens
                var tokenRequest = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("redirect_uri", redirectUri),
                    new KeyValuePair<string, string>("grant_type", "authorization_code")
                });

                var tokenResponse = await httpClient.PostAsync("https://oauth2.googleapis.com/token", tokenRequest);
                if (!tokenResponse.IsSuccessStatusCode)
                {
                    return BadRequest("Error retrieving access token.");
                }

                var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
                var tokenResult = JsonSerializer.Deserialize<GoogleTokenResponse>(tokenContent);

                // Step 2: Fetch user info from Google API
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);
                var userInfoResponse = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");

                if (!userInfoResponse.IsSuccessStatusCode)
                {
                    return BadRequest("Error retrieving user info.");
                }

                var userInfoContent = await userInfoResponse.Content.ReadAsStringAsync();
                var userInfo = JsonSerializer.Deserialize<GoogleUserInfo>(userInfoContent);

                // Step 3: Check if user exists in DB
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userInfo.Email);
                if (user == null)
                {
                    return Unauthorized($"The email '{userInfo.Email}' could be not found on the result processing system.");
                }

                // Step 4: Generate JWT token
                var jwtHelper = new JwtHelper();
                var token = jwtHelper.GenerateJwtToken(user);

                //Check the role of the authenticated user
               // Check the role of the authenticated user
                var userRoles = await _context.UserRoles
                    .Where(ur => ur.UserId == user.Id)
                    .Select(ur => ur.RoleName.RoleName) // Assuming there's a navigation property to Role
                    .ToListAsync();
                // 5. Set JWT token in an HttpOnly cookie
                Response.Cookies.Append("jwt", token, new CookieOptions
                {
                    HttpOnly = true,    // Prevents access to cookie from JavaScript
                    Secure = true,      // Ensures the cookie is only sent over HTTPS
                    SameSite = SameSiteMode.Lax,  // Mitigates CSRF attacks
                    Expires = DateTime.UtcNow.AddHours(1) // Set expiration as needed
                });

                // 6. Set UserRole in a separate cookie
                if (userRoles.Any()) // Ensure the list is not empty
                {
                    var rolesString = string.Join(",", userRoles); // Convert list to a comma-separated string

                    Response.Cookies.Append("UserRole", rolesString, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Lax,
                        Expires = DateTime.UtcNow.AddHours(1)
                    });
                }
                return Redirect("/dashboard");
            }
        }
        
        [HttpPost("create-role")]
        public async Task<IActionResult> CreateRole([FromBody] RoleDto r)
        {
            // Check if email already exists
            if (await _context.Roles.AnyAsync(u => u.RoleName == r.name))
            {
                return Conflict("Role already exists.");
            }

            // Map UserCreateDto to User model
            var role = new Roles
            {
                RoleName = r.name
            };

            // Add the user to the database
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            // Return a created response with the user
            return Ok(role);
        }
        
        [HttpPost]
        [Route("logout")]
        public async Task<IActionResult> Logout()
        {
            // Sign out the user
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Clear the authentication cookie manually (optional)
            Response.Cookies.Delete("jwt");
            Response.Cookies.Delete("UserRole");

            // Redirect to login or home page
            return Redirect("/");
        }
}   
    public class UserCreateDto
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string? Department { get; set; }
        public int DepartmentId { get; set; }
        public int FacultyId { get; set; }
    }

    public class RoleDto
    {
        public string name { get; set; }
    }

    public class UserRoleDto
    {
        public string user { get; set; }
        public string role { get; set; }
    }
    
    // DTOs for deserialization
    public class GoogleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonPropertyName("id_token")]
        public string IdToken { get; set; }
    }

    public class GoogleUserInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("verified_email")]
        public bool VerifiedEmail { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("given_name")]
        public string GivenName { get; set; }

        [JsonPropertyName("family_name")]
        public string FamilyName { get; set; }

        [JsonPropertyName("picture")]
        public string Picture { get; set; }

        [JsonPropertyName("locale")]
        public string Locale { get; set; }
    }

    public class EditUserRoleDto
    {
        public string Id { get; set; }
        public string Role { get; set; }
    }

}