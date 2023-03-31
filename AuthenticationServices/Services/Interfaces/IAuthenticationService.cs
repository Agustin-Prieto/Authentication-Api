using AuthenticationServices.Models;
using AuthenticationServices.Models.DTOs;

namespace AuthenticationServices.Services.Interfaces;

public interface IAuthenticationService
{
    Task<AuthResult> Register(RegisterRequestDto request);
    Task<AuthResult> Login(LoginRequestDto request);
    Task<AuthResult> RefreshToken(TokenRequest tokenRequest);
}