using AutoMapper;
using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RestaurantPOS.Db;
using RestaurantPOS.Models;
using RestaurantPOS.Models.Requests;
using RestaurantPOS.Models.Response;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.IO;

namespace RestaurantPOS.Controllers
{
    [ApiController]
    [Route("[controller]")]
    // [Authorize(Roles = "Admin,Commercial")]
    [EnableCors("CorsPolicy")]
    public class AuthController : ControllerBase
    {

        private readonly DbConfig _dbConfig;
        private readonly ILogger<AuthController> _logger;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public AuthController(ILogger<AuthController> logger, DbConfig dbConfig, IMapper mapper, IConfiguration configuration)
        {
            _logger = logger;
            _dbConfig = dbConfig;
            _mapper = mapper;
            _configuration = configuration;
        }

        // Add User
        [AllowAnonymous]
        [HttpPost("RegisterUser")]
        public async Task<ActionResult<GlobalResponse<User>>> RegisterUser([FromForm] UserRequest request)
        {
            var user = await _dbConfig.Users.FirstOrDefaultAsync(x => x.PhoneNumber == request.PhoneNumber && x.IsDeleted == false);
            if (user != null)
            {
                return BadRequest(new GlobalResponse<User>
                {
                    Data = user,
                    ErrorStatus = true,
                    Message = "phone number is already exsit"
                });
            }
            // Validate password is provided
            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new GlobalResponse<User>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "كلمة المرور مطلوبة"
                });
            }

            var newUse = _mapper.Map<User>(request);
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            newUse.Password = passwordHash;
            newUse.Role = "Commercial";
            
            // Upload logo if provided
            if (request.Logo != null)
            {
                newUse.Logo = await UploadImagesAsync(request.Logo);
            }
            
            // Set restaurant name
            newUse.RestaurantName = request.RestaurantName;
            
            _dbConfig.Users.Add(newUse);
            await _dbConfig.SaveChangesAsync();

            return Ok(new GlobalResponse<User>
            {
                Data = newUse,
                ErrorStatus = false,
                Message = "done"
            });
        }

        private async Task<string> UploadImagesAsync(IFormFile imageFile)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var validImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(imageFile.FileName);
            if (!validImageExtensions.Contains(fileExtension.ToLower()))
            {
                throw new InvalidOperationException("not a valid image extension");
            }

            var fileName = Guid.NewGuid().ToString() + fileExtension;
            var filePath = Path.Combine(path, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return fileName;
        }


        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] Login user)
        {
            var userFromDb = await _dbConfig.Users.FirstOrDefaultAsync(u => u.PhoneNumber == user.PhoneNumber);
            if(userFromDb == null)
            {
                return BadRequest(new GlobalResponse<User>
                {
                    Data = null,
                    ErrorStatus = false,
                    Message = "error in login info"

                });
            }
            if (!BCrypt.Net.BCrypt.Verify(user.Password, userFromDb.Password))
            {
                return BadRequest(new GlobalResponse<User>
                {
                    Data = null,
                    ErrorStatus = false,
                    Message = "error in login info"

                });
            }
            var claims = new[]
            {
                new Claim(ClaimTypes.MobilePhone, userFromDb!.PhoneNumber),
                new Claim(ClaimTypes.Role, userFromDb!.Role),
                new(ClaimTypes.NameIdentifier, userFromDb.Id.ToString()),
            };
            
            var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var jwtSecretKey = configuration["JwtSettings:SecretKey"] 
                ?? throw new InvalidOperationException("JWT Secret Key is not configured");
            var jwtIssuer = configuration["JwtSettings:Issuer"] ?? "Issuer";
            var jwtAudience = configuration["JwtSettings:Audience"] ?? "Audience";
            var expirationDays = int.Parse(configuration["JwtSettings:ExpirationInDays"] ?? "30");
            
            var token = new JwtSecurityToken
                   (
                       claims: claims,
                       expires: DateTime.UtcNow.AddDays(expirationDays),
                       notBefore: DateTime.UtcNow,
                       audience: jwtAudience,
                       issuer: jwtIssuer,
                       signingCredentials: new SigningCredentials(
                           new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
                           SecurityAlgorithms.HmacSha256)
            );
            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                role = userFromDb.Role, 
                info = userFromDb
            });
        }

    }

}
