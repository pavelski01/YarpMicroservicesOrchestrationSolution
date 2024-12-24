using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ApiGateway.Controllers;

[Route("api/[controller]")]
[ApiController]
public class IdentityController(UserManager<IdentityUser> userManager,
    RoleManager<IdentityRole> roleManager, IConfiguration configuration) : ControllerBase
{
    [HttpPost("{email}/{password}")]
    public async Task<IActionResult> Authenticate(string email, string password)
    {
        var user = await userManager.FindByNameAsync(email);
        if (user is null)
        {
            return Ok(await Register(email, password));
        }
        else
        {
            return Ok(await Login(user));
        }
    }

    private async Task<string> Login(IdentityUser user)
    {
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Authentication:Key"] ?? throw new InvalidDataException("Key not found")));
        var credential = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var role = (await userManager.GetRolesAsync(user))[0];
        var userClaims = new[]
        {
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Name, user.Email!),
            new Claim(ClaimTypes.Role, role),
            new Claim("vip", true.ToString())
        };
        var token = new JwtSecurityToken(
            issuer: configuration["Authentication:Issuer"],
            audience: configuration["Authentication:Audience"],
            claims: userClaims,
            expires: null,
            signingCredentials: credential);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> Register(string email, string password)
    {
        var result = await userManager.CreateAsync(new IdentityUser
        {
            Email = email,
            UserName = email,
            PasswordHash = password
        });

        if (result.Succeeded)
        {
            var adminRole = await roleManager.FindByNameAsync("Admin");
            if (adminRole is null)
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
                await userManager.AddToRoleAsync(
                    await userManager.FindByEmailAsync(email) ?? throw new InvalidDataException("Admin not found"),
                    "Admin");
            }
            else
            {
                var userRole = await roleManager.FindByNameAsync("User");
                if (userRole is null)
                {
                    await roleManager.CreateAsync(new IdentityRole("User"));
                    await userManager.AddToRoleAsync(
                        await userManager.FindByEmailAsync(email) ?? throw new InvalidDataException("User not found"),
                        "User");
                }
            }
            return "Successful";
        }
        return "Unsuccessful";
    }
}
