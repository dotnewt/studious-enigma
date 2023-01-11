using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using studious_enigma.Models;

namespace studious_enigma.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        public IConfiguration _configuration;
        public UserManager<User> _userManager;

        public AuthController(IConfiguration configuration, UserManager<User> userManager)
        {
            _configuration = configuration;
            _userManager = userManager;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequestErrorMessage();
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            var isAuthorized = user != null && await _userManager.CheckPasswordAsync(user, request.Password);


            if (isAuthorized)
            {
                var authResponse = await GetTokens(user);
                user.RefreshToken = authResponse.RefreshToken;
                await _userManager.UpdateAsync(user);
                return Ok(authResponse);
            }
            else
            {
                return Unauthorized("Invalid credentials");
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequestErrorMessage();
            }

            var principal = GetPrincipalFromExpiredToken(request.AccessToken);
            var userEmail = principal.FindFirstValue("Email");

            var user = !string.IsNullOrEmpty(userEmail) ? await _userManager.FindByEmailAsync(userEmail) : null;
            if (user == null || user.RefreshToken != request.RefreshToken)
            {
                return BadRequest("Invalid refresh token");
            }

            var response = await GetTokens(user);
            user.RefreshToken = response.RefreshToken;
            await _userManager.UpdateAsync(user);
            return Ok(response);
        }

        [HttpPost("revoke")]
        [Authorize]
        public async Task<IActionResult> Revoke(RevokeRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequestErrorMessage();
            }

            var userEmail = this.HttpContext.User.FindFirstValue("Email");

            var user = !string.IsNullOrEmpty(userEmail) ? await _userManager.FindByEmailAsync(userEmail) : null;
            if (user == null || user.RefreshToken != request.RefreshToken)
            {
                return BadRequest("Invalid refresh token");
            }

            user.RefreshToken = null;
            await _userManager.UpdateAsync(user);
            return Ok("Refresh token is revoked");
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest registerRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequestErrorMessage();
            }

            var isEmailAlreadyRegistered = await _userManager.FindByEmailAsync(registerRequest.Email) != null;
            var isUserNameAlreadyRegistered = await _userManager.FindByNameAsync(registerRequest.UserName) != null;

            if (isEmailAlreadyRegistered)
            {
                return Conflict($"Email Id {registerRequest.Email} is already registered");
            }

            if (isUserNameAlreadyRegistered)
            {
                return Conflict($"Username {registerRequest.UserName} is already registered");
            }

            var newUser = new User
            {
                Email = registerRequest.Email,
                UserName = registerRequest.UserName,
                DisplayName = registerRequest.DisplayName
            };

            var result = await _userManager.CreateAsync(newUser, registerRequest.Password);

            if (result.Succeeded)
            {
                return Ok("User created successfully");
            }
            else
            {
                return StatusCode(500, result.Errors.Select(e => new { Msg = e.Code, Desc = e.Description }).ToList());
            }
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["token:key"])),
                ValidateLifetime = false
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");
            return principal;
        }

        private async Task<AuthResponse> GetTokens(User user)
        {
            var claims = new[]{
                            new Claim(JwtRegisteredClaimNames.Sub, _configuration["token:subject"]),
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                            new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString()),
                            new Claim("Id", user.Id),
                            new Claim("UserName", user.UserName),
                            new Claim("Email", user.Email)
                };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["token:key"]));
            var singIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                _configuration["token:issuer"],
                _configuration["token:audience"],
                claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["token:accessTokenExpiryMinutes"])),
                signingCredentials: singIn
            );

            var tokeStr = new JwtSecurityTokenHandler().WriteToken(token);
            var refreshTokenStr = GetRefreshToken();
            user.RefreshToken = refreshTokenStr;

            var authResponse = new AuthResponse { AccessToken = tokeStr, RefreshToken = refreshTokenStr };
            return await Task.FromResult(authResponse);
        }

        private string GetRefreshToken()
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var tokesIsUnique = !_userManager.Users.Any(t => t.RefreshToken == token);

            if (!tokesIsUnique)
            {
                return GetRefreshToken();
            }

            return token;
        }

        private IActionResult BadRequestErrorMessage()
        {
            var errMsgs = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
            return BadRequest(errMsgs);
        }
    }
}