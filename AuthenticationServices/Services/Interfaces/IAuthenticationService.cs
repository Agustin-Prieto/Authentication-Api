﻿using AuthenticationServices.Models;
using AuthenticationServices.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthenticationServices.Services.Interfaces;

public interface IAuthenticationService
{
    Task<AuthResult> Register(RegisterRequestDto request);
    Task<AuthResult> Login(LoginRequestDto request);
    Task<AuthResult> RefreshToken(TokenRequest tokenRequest);
}