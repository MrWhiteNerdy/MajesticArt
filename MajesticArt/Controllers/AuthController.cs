﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using MajesticArt.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace MajesticArt.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;

        public AuthController(
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            this.configuration = configuration;
            this.userManager = userManager;
            this.signInManager = signInManager;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var user = await userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                return BadRequest("Invalid username or password");
            }

            var result = await signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

            ClaimsIdentity identity = await GetClaimsIdentity(user);

            if (result.Succeeded)
            {
                var roles = await userManager.GetRolesAsync(user);
                var loginResponseDto = new LoginResponseDto
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Roles = roles,
                    Token = GenerateToken(identity)
                };
                return Ok(loginResponseDto);
            }

            return BadRequest("Invalid username or password");
        }

        [Route("update/email")]
        [HttpPut]
        public async Task<IActionResult> UpdateEmail(UpdateEmailDto updateEmailDto)
        {
            var user = await userManager.FindByEmailAsync(updateEmailDto.Email);

            if (user == null)
            {
                return NotFound();
            }

            var email = HttpContext.User.FindFirstValue(ClaimTypes.Email);

            if (email != updateEmailDto.Email)
            {
                return Unauthorized();
            }

            user.Email = updateEmailDto.NewEmail;
            user.UserName = updateEmailDto.NewEmail;

            var result = await userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Ok();
            }

            return BadRequest();
        }

        [Route("update/password")]
        [HttpPut]
        public async Task<IActionResult> UpdatePassword(UpdatePasswordDto updatePasswordDto)
        {
            var user = await userManager.FindByEmailAsync(updatePasswordDto.Email);
            if (user == null)
            {
                return NotFound();
            }

            var email = HttpContext.User.FindFirstValue(ClaimTypes.Email);

            if (email != updatePasswordDto.Email)
            {
                return Unauthorized();
            }

            var result = await userManager.ChangePasswordAsync(user, updatePasswordDto.Password, updatePasswordDto.NewPassword);

            if (result.Succeeded)
            {
                return Ok();
            }

            return BadRequest();
        }

        private async Task<ClaimsIdentity> GetClaimsIdentity(ApplicationUser user)
        {
            var roles = await userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email)
            };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            return new ClaimsIdentity(claims, "Token");
        }

        private string GenerateToken(ClaimsIdentity identity)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:SecretKey"]));
            var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: configuration["JWT:Issuer"],
                audience: configuration["JWT:Audience"],
                claims: identity.Claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.Add(TimeSpan.FromDays(1)),
                signingCredentials);

            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            return encodedJwt;
        }
    }
}