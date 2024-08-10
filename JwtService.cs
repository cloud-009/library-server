using library_server.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace library_server
{
    public class JwtService
    {
        private readonly string key = string.Empty;
        private readonly int duration;

        public JwtService(IConfiguration configuration)
        {
            key = configuration["Jwt:Key"]!;
            duration = int.Parse(configuration["Jwt:Duration"]!);
        }

        public string GenerateToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.key));
            var signingKey = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("id", user.Id.ToString()),
                new Claim("firstName", user.FirstName),
                new Claim("lastName", user.LastName),
                new Claim("email", user.Email),
                new Claim("mobileNumber", user.MobileNumber),
                new Claim("userType", user.UserType.ToString()),
                new Claim("accountStatus", user.AccountStatus.ToString()),
                new Claim("createdOn", user.createdOn.ToString()),
            };

            var jwtToken = new JwtSecurityToken(
                issuer: "localhost",
                audience: "localhost",
                claims: claims,
                notBefore: DateTime.Now,
                expires: DateTime.Now.AddMinutes(this.duration),
                signingKey);
            var token = new JwtSecurityTokenHandler().WriteToken(jwtToken);
            Console.WriteLine($"token: { token}");
            return token;
        }
    }
}