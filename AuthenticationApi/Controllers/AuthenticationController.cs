using AuthenticationServices.Models;
using AuthenticationServices.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationApi.Controllers;

public class AuthenticationController : ControllerBase
{
    private readonly AuthenticationServices.Services.Interfaces.IAuthenticationService _authenticationService;

    public AuthenticationController(AuthenticationServices.Services.Interfaces.IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    [HttpPost]
    [Route(template: "api/v1/register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResult()
            {
                Errors = ModelState.Values.SelectMany(x => x.Errors.Select(xx => xx.ErrorMessage)).ToList(),
                Result = false
            });
        }

        var response = await _authenticationService.Register(request);

        if (response.Errors != null && response.Errors.Any())  
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost]
    [Route(template: "api/v1/login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResult()
            {
                Errors = ModelState.Values.SelectMany(x => x.Errors.Select(xx => xx.ErrorMessage)).ToList()
            });
        }

        var response = await _authenticationService.Login(request);

        if (response.Errors != null && response.Errors.Any())
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost]
    [Route(template: "api/v1/refreshToken")]
    public async Task<IActionResult> RefreshToken([FromBody] TokenRequest tokenRequest)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResult()
            {
                Errors = ModelState.Values.SelectMany(x => x.Errors.Select(xx => xx.ErrorMessage)).ToList(),
                Result = false
            });
        }
        
        var response = await _authenticationService.RefreshToken(tokenRequest);

        if(response == null)
        {
            return BadRequest(new AuthResult()
            {
                Errors = new List<string>()
                {
                    "Invalid Tokens"
                },
                Result = false
            });
        }

        if (response.Errors != null)
        {
            return BadRequest(response);
        }
        
        return Ok(response);
    }
}
