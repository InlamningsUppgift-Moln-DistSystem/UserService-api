using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserMicroService.DTOs;
using UserMicroService.Services;
using System.Threading.Tasks;

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

        [HttpPatch("me")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var (success, errors) = await _userService.UpdateUserAsync(userId, request);
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

        [HttpPut("me/upload-profile-image")]
        public async Task<IActionResult> UploadProfileImage([FromForm] IFormFile file)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var imageUrl = await _userService.UploadProfileImageAsync(userId, file, deleteOldImage: true);
                return Ok(new { imageUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string email)
        {
            var success = await _userService.ConfirmEmailAsync(email);
            return success ? Ok("Email confirmed.") : BadRequest("Invalid or already confirmed.");
        }
    }
}
