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
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using studious_enigma.Models;

namespace studious_enigma.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        public static List<User> tempUserDb = new List<User>{
            new User{UserId="abc123",UserName="John", DisplayName="BilboBaggins", Email="john@abc.com", Password="john@123" },
            new User{UserId="def456",UserName="Jane", DisplayName="Galadriel", Email="jane@xyz.com", Password="jane1990" }
        };

        public IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequestErrorMessage();
            }

            var user = await GetUser(request.Email, request.Password);

            if (user != null)
            {
                var authResponse = await GetTokens(user);
                return Ok(authResponse);
            }
            else
            {
                return BadRequest("Invalid credentials");
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequestErrorMessage();
            }

            var user = await GetUserByRefreshToken(request.RefreshToken);

            if (user == null)
            {
                return BadRequest("Invalid refresh token");
            }

            var response = await GetTokens(user);
            return Ok(response);
        }

        [HttpPost("revoke")]
        public async Task<IActionResult> Revoke(RevokeRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequestErrorMessage();
            }

            //check if any user with this refresh token exists
            var user = await GetUserByRefreshToken(request.RefreshToken);
            if (user == null)
            {
                return BadRequest("Invalid refresh token");
            }

            //remove refresh token 
            user.RefreshToken = null;

            return Ok("Refresh token is revoked");
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest registerRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequestErrorMessage();
            }

            var isEmailAlreadyRegistered = await GetUserByEmail(registerRequest.Email) != null;

            if (isEmailAlreadyRegistered)
            {
                return Conflict($"Email Id {registerRequest.Email} is already registered");
            }

            await AddUser(new User
            {
                Email = registerRequest.Email,
                UserName = registerRequest.UserName,
                Password = registerRequest.Password
            });

            return Ok("User created successfully");
        }

        private async Task<User> GetUserByEmail(string email)
        {
            return await Task.FromResult(tempUserDb.FirstOrDefault(u => u.Email == email));
        }

        private async Task<User> AddUser(User newUser)
        {
            newUser.UserId = $"user{DateTime.Now.ToString("hhmmss")}";
            tempUserDb.Add(newUser);
            return newUser;
        }

        private async Task<User> GetUserByRefreshToken(string refreshToken)
        {
            return await Task.FromResult(tempUserDb.FirstOrDefault(u => u.RefreshToken == refreshToken));
        }

        private async Task<AuthResponse> GetTokens(User user)
        {
            //create claims details based on the user information
            var claims = new[]{
                            new Claim(JwtRegisteredClaimNames.Sub, _configuration["token:subject"]),
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                            new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString()),
                            new Claim("UserId", user.UserId),
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
            var tokesIsUnique = !tempUserDb.Any(t => t.RefreshToken == token);

            if (!tokesIsUnique)
            {
                return GetRefreshToken();
            }

            return token;
        }

        [HttpGet("tokenValidate")]
        [Authorize]
        public async Task<IActionResult> TokenValidate()
        {
            //This endpoint is created so any user can validate their token
            return Ok("Token is valid");
        }

        private async Task<User> GetUser(string email, string password)
        {
            return await Task.FromResult(tempUserDb.FirstOrDefault(u => u.Email == email && u.Password == password));
        }

        private IActionResult BadRequestErrorMessage()
        {
            var errMsgs = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
            return BadRequest(errMsgs);
        }
    }
}