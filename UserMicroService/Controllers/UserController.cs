using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserMicroService.DTOs;
using UserMicroService.Services;

namespace UserMicroService.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        private string? GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var user = await _userService.GetUserAsync(userId);
            return user != null ? Ok(user) : NotFound();
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile(UpdateUserRequest request)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var success = await _userService.UpdateUserAsync(userId, request);
            return success ? NoContent() : NotFound();
        }

        [HttpDelete("me")]
        public async Task<IActionResult> DeleteMyProfile()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var success = await _userService.DeleteUserAsync(userId);
            return success ? NoContent() : NotFound();
        }
    }
}
