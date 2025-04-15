using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt; // For JWT handling
using System.Security.Claims;        // For Claims
using Microsoft.IdentityModel.Tokens; // For SymmetricSecurityKey and SigningCredentials
using System.Text;                   // For Encoding
using rps.Models;                    // Assuming your User class is in the "rps.Models" namespace

public class JwtHelper
{
    private readonly string _secretKey = "0$H1OM#0L3LargerKeyWithEnoughLengthToPassValidation12345"; // 64 characters // Secret key used to sign the JWT
    private readonly string _issuer = "EUI_RPS"; // Set your issuer (usually your app or API name)
    private readonly string _audience = "https://result.edouniversity.edu.ng"; // Set the audience (who the token is intended for)

    public string GenerateJwtToken(User user)
    {
        // Define the key used to sign the token
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Claims: Information you want to store in the JWT
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Name),              // User's name
            new Claim(ClaimTypes.Email, user.Email),            // User's email
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // User's ID as unique identifier
        };

        // Create the JWT token with the claims and expiration time
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.Now.AddHours(1), // Set expiration time
            signingCredentials: credentials
        );

        // Generate the token string
        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }

    public static IDictionary<string, string> DecodeJwt(string token)
    {
        if (string.IsNullOrEmpty(token))
            throw new ArgumentNullException(nameof(token));

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Get claims from the token
        var claims = jwtToken.Claims.ToDictionary(c => c.Type, c => c.Value);
        var normalizedClaims = new Dictionary<string, string>();

        foreach (var claim in claims)
        {
            string key = claim.Key switch
            {
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress" => "email",
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name" => "name",
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" => "id",
                _ => claim.Key
            };
            
            normalizedClaims[key] = claim.Value;
        }

        return normalizedClaims;
    }
}
