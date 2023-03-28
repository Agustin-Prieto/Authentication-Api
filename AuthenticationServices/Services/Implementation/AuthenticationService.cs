using AuthenticationServices.Models;
using AuthenticationServices.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
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

    public AuthenticationService(UserManager<IdentityUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<AuthResult> Register(RegisterRequestDto request)
    {
        var connectionStr = _configuration.GetConnectionString("DefaultConnection");
        Console.WriteLine(connectionStr);
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

        var token = GenerateJwtToken(existingUser);

        return new AuthResult()
        {
            Result = true,
            Token = token
        };
    }

    private string GenerateJwtToken(IdentityUser user)
    {
        var key = Encoding.UTF8.GetBytes(_configuration.GetSection("JwtConfig:Secret").Value);
        var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(type:"Id", value:user.Id),
            new Claim(type:JwtRegisteredClaimNames.Sub, value:user.Email),
            new Claim(type:JwtRegisteredClaimNames.Email, value:user.Email),
            new Claim(type:JwtRegisteredClaimNames.Jti, value:Guid.NewGuid().ToString()),
            new Claim(type:JwtRegisteredClaimNames.Iat, value:DateTime.Now.ToUniversalTime().ToString())
        };

        var token = new JwtSecurityToken(
                issuer: "https://localhost:5001",
                audience: "https://localhost:5001",
                expires: DateTime.Now.AddMinutes(5),
                claims: new List<Claim>(),
                signingCredentials: credentials
            );

        var encodedToken = new JwtSecurityTokenHandler().WriteToken(token);

        return encodedToken;
    }
}