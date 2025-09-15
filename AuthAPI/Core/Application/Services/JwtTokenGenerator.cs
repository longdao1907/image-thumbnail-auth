using AuthAPI.Core.Application.Interfaces;
using AuthAPI.Core.Application.Options;
using AuthAPI.Core.Domain.Entities;
using Google.Cloud.SecretManager.V1;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthAPI.Core.Application.Services
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {

        private readonly JwtOptions _jwtOptions;
        private readonly IConfiguration _configuration;
        public JwtTokenGenerator(IOptions<JwtOptions> jwtOptions, IConfiguration configuration)
        {
            _jwtOptions = jwtOptions.Value;
            _configuration = configuration;
        }

        public string GenerateToken(ApplicationUser applicationUser)
        {
            //Get secret from GCP Secret Manager
            string projectId = _configuration.GetSection("Gcp").GetValue<string>("ProjectID") ?? throw new ArgumentNullException("Gcp Project ID not configured.");
            string jwtSecret = _configuration.GetSection("Gcp").GetValue<string>("JWTSecretKey") ?? throw new ArgumentNullException("Gcp JWTSecretKey not configured.");
            string secretVersion = _configuration.GetSection("Gcp").GetValue<string>("SecretVersion") ?? throw new ArgumentNullException("Gcp Secret Version not configured.");

            //Init Secret Manager Client
            SecretManagerServiceClient client = SecretManagerServiceClient.Create();

            //get the secret value for JWT creation
            SecretVersionName secretVersionName = new(projectId, jwtSecret, secretVersion);
            AccessSecretVersionResponse result = client.AccessSecretVersion(secretVersionName);
            _jwtOptions.Secret = result.Payload.Data.ToStringUtf8();


            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtOptions.Secret);

            var claimList = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Email,applicationUser.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Sub,applicationUser.Id),
                new Claim(JwtRegisteredClaimNames.Name,applicationUser.UserName ?? string.Empty)
            };


            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Audience = _jwtOptions.Audience,
                Issuer = _jwtOptions.Issuer,
                Subject = new ClaimsIdentity(claimList),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public string GenerateServiceToken(string clientId, string clientSecret)
        {
            //Get secret from GCP Secret Manager
            string projectId = _configuration.GetSection("Gcp").GetValue<string>("ProjectID") ?? throw new ArgumentNullException("Gcp Project ID not configured.");
            string keyClientID = _configuration.GetSection("Gcp").GetValue<string>("Client-ID") ?? throw new ArgumentNullException("Gcp ClientID not configured.");
            string keyClientSecret = _configuration.GetSection("Gcp").GetValue<string>("Client-Secret") ?? throw new ArgumentNullException("Gcp ClientSecret not configured.");
            string jwtSecret = _configuration.GetSection("Gcp").GetValue<string>("JWTSecretKey") ?? throw new ArgumentNullException("Gcp JWTSecretKey not configured.");
            string secretVersion = _configuration.GetSection("Gcp").GetValue<string>("SecretVersion") ?? throw new ArgumentNullException("Gcp Secret Version not configured.");

            //Init Secret Manager Client
            SecretManagerServiceClient client = SecretManagerServiceClient.Create();

            //get the secret value for JWT creation
            SecretVersionName secretVersionName = new(projectId, keyClientID, secretVersion);
            AccessSecretVersionResponse result = client.AccessSecretVersion(secretVersionName);
            string validClientID = result.Payload.Data.ToStringUtf8();

            secretVersionName = new(projectId, keyClientSecret, secretVersion);
            result = client.AccessSecretVersion(secretVersionName);
            string validClientSecret = result.Payload.Data.ToStringUtf8();

            if (clientId != validClientID || clientSecret != validClientSecret)
            {
                throw new UnauthorizedAccessException("Invalid client credentials.");
            }

            secretVersionName = new(projectId, jwtSecret, secretVersion);
            result = client.AccessSecretVersion(secretVersionName);
            _jwtOptions.Secret = result.Payload.Data.ToStringUtf8();

            var issuer = _configuration.GetSection("ServiceTokenOptions").GetValue<string>("Issuer") ?? throw new ArgumentNullException("ServiceTokenOptions Issuer not configured.");
            var audience = _configuration.GetSection("ServiceTokenOptions").GetValue<string>("Audience") ?? throw new ArgumentNullException("ServiceTokenOptions Audience not configured.");

            var key = Encoding.UTF8.GetBytes(_jwtOptions.Secret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("Id", Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Sub, clientId),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    // **QUAN TRỌNG**: Thêm một claim đặc biệt để phân biệt đây là service token
                    new Claim("token_type", "service")
                }),
                Expires = DateTime.UtcNow.AddMinutes(30), // Token có hiệu lực 30 phút
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
