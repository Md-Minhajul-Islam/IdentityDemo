using IdentityDemo.DTOs;
using IdentityDemo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(UserManager<AppUser> userManager,
                           RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // Create a new role
    [HttpPost("create-role")]
    public async Task<IActionResult> CreateRole([FromBody] string roleName)
    {
        if (await _roleManager.RoleExistsAsync(roleName))
            return BadRequest("Role already exists");

        var result = await _roleManager.CreateAsync(new IdentityRole(roleName));

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok($"Role '{roleName}' created successfully!");
    }

    // Assign a role to a user
    [HttpPost("assign-role")]
    public async Task<IActionResult> AssignRole(AssignRoleDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return NotFound("User not found");

        if (!await _roleManager.RoleExistsAsync(dto.Role))
            return BadRequest("Role does not exist");

        var result = await _userManager.AddToRoleAsync(user, dto.Role);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok($"Role '{dto.Role}' assigned to '{dto.Email}'");
    }


    // Assign a custom claim to a user
    [HttpPost("assign-claim")]
    public async Task<IActionResult> AssignClaim(string email, 
                                                string claimType, 
                                                string claimValue)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return NotFound("User not found");

        var existingClaims = await _userManager.GetClaimsAsync(user);
        var existing = existingClaims
            .FirstOrDefault(c => c.Type == claimType);

        // Remove old claim if exists
        if (existing != null)
            await _userManager.RemoveClaimAsync(user, existing);

        var result = await _userManager.AddClaimAsync(
            user, new System.Security.Claims.Claim(claimType, claimValue));

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok($"Claim '{claimType}:{claimValue}' assigned to '{email}'");
    }

    // Get all users (admin only)
    [HttpGet("users")]
    public IActionResult GetUsers()
    {
        var users = _userManager.Users
            .Select(u => new { u.Id, u.Email })
            .ToList();

        return Ok(users);
    }


}