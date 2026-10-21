using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrivilegeDesk.Api.Data;
using PrivilegeDesk.Api.Dtos;
using PrivilegeDesk.Api.Services;

namespace PrivilegeDesk.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtTokenService _jwt;
    private readonly AuditService _audit;

    public AuthController(AppDbContext db, JwtTokenService jwt, AuditService audit)
    {
        _db = db;
        _jwt = jwt;
        _audit = audit;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized("Invalid username or password.");
        }

        var token = _jwt.CreateToken(user);
        await _audit.WriteAsync(user.Id, "Login", $"User '{user.Username}' logged in.");

        return Ok(new LoginResponse
        {
            Token = token,
            Username = user.Username,
            Role = user.Role.ToString()
        });
    }
}
