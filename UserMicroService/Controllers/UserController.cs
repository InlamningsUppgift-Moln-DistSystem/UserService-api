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

        [HttpPut("me/username")]
        public async Task<IActionResult> UpdateUsername([FromBody] string username)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var (success, errors) = await _userService.UpdateUsernameAsync(userId, username);
            return success ? NoContent() : BadRequest(errors);
        }

        [HttpPut("me/email")]
        public async Task<IActionResult> UpdateEmail([FromBody] string email)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var (success, errors) = await _userService.UpdateEmailAsync(userId, email);
            return success ? NoContent() : BadRequest(errors);
        }

        [HttpPut("me/password")]
        public async Task<IActionResult> UpdatePassword(UpdatePasswordRequest request)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var (success, errors) = await _userService.UpdatePasswordAsync(userId, request);
            return success ? NoContent() : BadRequest(errors);
        }

        [HttpDelete("me")]
        public async Task<IActionResult> DeleteMyProfile()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var success = await _userService.DeleteUserAsync(userId);
            return success ? NoContent() : NotFound();
        }

        [HttpPost("me/upload-profile-image")]
        public async Task<IActionResult> UploadProfileImage(IFormFile file)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var imageUrl = await _userService.UploadProfileImageAsync(userId, file);
                return Ok(new { imageUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}