using AuthenticationData.Data;
using AuthenticationServices.Models;
using AuthenticationServices.Models.DTOs;
using AuthenticationServices.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthenticationServices.Services.Implementation;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;
    private readonly TokenValidationParameters _tokenValidationParameters;

    public AuthenticationService(UserManager<IdentityUser> userManager, IConfiguration configuration, AppDbContext context, TokenValidationParameters tokenValidationParameters)
    {
        _userManager = userManager;
        _configuration = configuration;
        _context = context;
        _tokenValidationParameters = tokenValidationParameters;
    }
    
    public async Task<AuthResult> Register(RegisterRequestDto request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return new AuthResult()
            {
                Errors = new List<string>() { "User with this email already exists" },
                Result = false
            };
        }

        var newUser = new IdentityUser()
        {
            Email = request.Email,
            UserName = request.Name
        };

        try
        {
            var isCreated = await _userManager.CreateAsync(newUser, request.Password);

            if (isCreated.Succeeded)
            {
                return new AuthResult()
                {
                    Result = true,
                };
            }
            else
            {
                return new AuthResult()
                {
                    Errors = isCreated.Errors.Select(x => x.Description).ToList(),
                    Result = false
                };
            }
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Se ha producido un error al crear el usuario: {ex.Message}");
            throw new ArgumentException("Se ha producido un error al crear el usuario", ex);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Se ha producido un error al crear el usuario: {ex.Message}");
            throw new InvalidOperationException("Se ha producido un error al crear el usuario", ex);
        }
        catch (AggregateException ex)
        {
            throw new Exception($"Se ha producido un error al crear el usuario: {ex.Message}", ex);
        }
    }

    public async Task<AuthResult> Login(LoginRequestDto request)
    {
        // validar que si existe usuario
        var existingUser = await _userManager.FindByEmailAsync(request.Email);

        if (existingUser == null)
        {
            return new AuthResult()
            {
                Errors = new List<string>() { "User with this email does not exists" },
                Result = false
            };
        }

        // validar que la contraseña es correcta
        var isCorrect = await _userManager.CheckPasswordAsync(existingUser, request.Password);

        if (!isCorrect)
        {
            return new AuthResult()
            {
                Errors = new List<string>() { "User or password is incorrect" },
                Result = false
            };
        }

        var JwtToken = await GenerateJwtToken(existingUser);

        return JwtToken;
    }

    public async Task<AuthResult> RefreshToken(TokenRequest tokenRequest)
    {
        var jwtTokenHandler = new JwtSecurityTokenHandler();
        try
        {
            _tokenValidationParameters.ValidateLifetime = false;
            var tokenInVerificaion = jwtTokenHandler.ValidateToken(tokenRequest.Token, _tokenValidationParameters, out var validatedToken);

            if (validatedToken is JwtSecurityToken jwtSecurityToken)
            {
                bool result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);

                if (result == null) return null;
            }

            var utcExpiryDate = long.Parse(tokenInVerificaion.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp).Value);
            var expiryDate = UnixTimeStampToDateTime(utcExpiryDate);

            if (expiryDate > DateTime.Now.ToUniversalTime())
            {
                return new AuthResult()
                {
                    Result = false,
                    Errors = new List<string>()
                    {
                        "Expired Token"
                    }
                };
            }

            var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == tokenRequest.RefreshToken);

            if (storedToken == null)
            {
                return new AuthResult()
                {
                    Result = false,
                    Errors = new List<string>()
                    {
                        "Invalid tokens"
                    }
                };
            }

            if (storedToken.IsUsed)
            {
                return new AuthResult()
                {
                    Result = false,
                    Errors = new List<string>()
                    {
                        "Invalid tokens: Token is used"
                    }
                };
            }

            if (storedToken.IsRevoke)
            {
                return new AuthResult()
                {
                    Result = false,
                    Errors = new List<string>()
                    {
                        "Invalid tokens: Token is revoked"
                    }
                };
            }

            var jti = tokenInVerificaion.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti).Value;

            if (storedToken.JwtId != jti)
            {
                return new AuthResult()
                {
                    Result = false,
                    Errors = new List<string>()
                    {
                        "Invalid tokens"
                    }
                };
            }

            if (storedToken.ExpiryDates < DateTime.UtcNow)
            {
                return new AuthResult()
                {
                    Result = false,
                    Errors = new List<string>()
                    {
                        "Expired Tokens"
                    }
                };
            }

            storedToken.IsUsed = true;

            _context.RefreshTokens.Update(storedToken);
            await _context.SaveChangesAsync();

            var dbUser = await _userManager.FindByIdAsync(storedToken.UserId);

            return await GenerateJwtToken(dbUser);
        }
        catch (Exception ex)
        {
            return new AuthResult()
            {
                Result = false,
                Errors = new List<string>()
                {
                    "Server Error"
                }
            };
        }
    }

    private DateTime UnixTimeStampToDateTime(long utcExpiryDate)
    {
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        return dateTime.AddSeconds(utcExpiryDate).ToUniversalTime();
    }

    private async Task<AuthResult> GenerateJwtToken(IdentityUser user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration.GetSection("JwtConfig:Secret").Value);

        var tokenDescriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new Claim(type:"Id", value:user.Id),
                new Claim(type:JwtRegisteredClaimNames.Sub, "Login"),
                new Claim(type:JwtRegisteredClaimNames.Email, value:user.Email),
                new Claim(type:JwtRegisteredClaimNames.Jti, user.Id),
                new Claim(type:JwtRegisteredClaimNames.Iat, value:DateTime.Now.ToUniversalTime().ToString()),
                new Claim(type:JwtRegisteredClaimNames.Iss, _configuration.GetSection("JwtConfig:Issuer").Value),
                new Claim(type:JwtRegisteredClaimNames.Aud, _configuration.GetSection("JwtConfig:Audience").Value),
                new Claim(type:JwtRegisteredClaimNames.Exp, _configuration.GetSection("JwtConfig:ExpiryInMinutes").Value),
            }),
            
            Expires = DateTime.UtcNow.AddMinutes(Double.Parse(_configuration.GetSection("JwtConfig:ExpiryInMinutes").Value)).ToUniversalTime(),

            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var encodedToken = tokenHandler.WriteToken(token);

        var refreshToken = new RefreshToken()
        {
            JwtId = user.Id,
            Token = RandomStringGen(23),
            UserId = user.Id,
            AddedDate = DateTime.UtcNow,
            ExpiryDates = DateTime.UtcNow.AddMonths(6),
            IsRevoke = false,
            IsUsed = false,
        };

        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        return new AuthResult()
        {
            Result = true,
            Token = encodedToken,
            RefreshToken = refreshToken.Token
        };
    }

    private string RandomStringGen(int length)
    {
        var random = new Random();
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        return new string(Enumerable.Repeat(chars, length)
          .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}