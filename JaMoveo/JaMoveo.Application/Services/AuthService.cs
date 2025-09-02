using JaMoveo.Application.Interfaces;
using JaMoveo.Core.DTOs;
using JaMoveo.Core.DTOs.Auth;
using JaMoveo.Infrastructure.Entities;
using JaMoveo.Infrastructure.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace JaMoveo.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly string _jwtKey;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole<int>> roleManager,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _jwtKey = configuration["Jwt:Key"];
            _jwtIssuer = configuration["Jwt:Issuer"];
            _jwtAudience = configuration["Jwt:Audience"];
            _logger = logger;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto loginRequest)
        {
            var user = await _userManager.FindByNameAsync(loginRequest.Username);

            if (user == null)
            {
                _logger.LogWarning("ניסיון התחברות נכשל - משתמש לא קיים: {Username}", loginRequest.Username);
                throw new UnauthorizedAccessException("שם משתמש או סיסמה לא נכונים");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginRequest.Password, false);

            if (!result.Succeeded)
            {
                _logger.LogWarning("ניסיון התחברות נכשל - סיסמה שגויה: {Username}", loginRequest.Username);
                throw new UnauthorizedAccessException("שם משתמש או סיסמה לא נכונים");
            }

            var userDto = await MapToUserDtoAsync(user);
            var token = await GenerateJwtTokenAsync(user);

            _logger.LogInformation("משתמש התחבר בהצלחה: {Username}", loginRequest.Username);

            return new LoginResponseDto
            {
                Token = token,
                User = userDto
            };
        }

        public async Task<bool> SignUpAsync(SignUpRequestDto signUpRequest)
        {
            return await CreateUserAsync(signUpRequest, UserRole.Player);
        }

        public async Task<bool> SignUpAdminAsync(SignUpRequestDto signUpRequest)
        {
            return await CreateUserAsync(signUpRequest, UserRole.Admin);
        }

        private async Task<bool> CreateUserAsync(SignUpRequestDto signUpRequest, UserRole role)
        {
            var existingUser = await _userManager.FindByNameAsync(signUpRequest.Username);
            if (existingUser != null)
            {
                throw new ArgumentException("שם המשתמש כבר קיים במערכת");
            }

            var user = new ApplicationUser
            {
                UserName = signUpRequest.Username,
                Role = role,
                Instrument = signUpRequest.Instrument,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, signUpRequest.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("שגיאה ביצירת משתמש {Username}: {Errors}", signUpRequest.Username, errors);
                throw new ArgumentException($"שגיאה ביצירת המשתמש: {errors}");
            }

            // הוספת תפקיד
            var roleName = role == UserRole.Admin ? "Admin" : "Player";
            await _userManager.AddToRoleAsync(user, roleName);

            _logger.LogInformation("משתמש חדש נוצר בהצלחה: {Username} בתפקיד {Role}", signUpRequest.Username, roleName);

            return true;
        }

        public async Task<bool> IsUsernameAvailableAsync(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            return user == null;
        }

        public async Task<UserDto> GetUserByIdAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            return user != null ? await MapToUserDtoAsync(user) : null;
        }

        public async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var roles = await _userManager.GetRolesAsync(user);
            var userClaims = await _userManager.GetClaimsAsync(user);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("instrument", user.Instrument.ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            claims.AddRange(userClaims);

            var token = new JwtSecurityToken(
                issuer: _jwtIssuer,
                audience: _jwtAudience,
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public bool ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtKey);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtAudience,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<UserDto> MapToUserDtoAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? user.Role.ToString();

            return new UserDto
            {
                Id = user.Id,
                Username = user.UserName,
                Role = primaryRole,
                Instrument = user.Instrument,
                CreatedAt = user.CreatedAt
            };
        }
    }
}