// IUserService.cs
using UserMicroService.DTOs;

namespace UserMicroService.Services
{
    public interface IUserService
    {
        Task<UserResponse?> GetUserAsync(string userId);
        Task<(bool success, Dictionary<string, string> errors)> UpdateUsernameAsync(string userId, string username);
        Task<(bool success, Dictionary<string, string> errors)> UpdateEmailAsync(string userId, string email);
        Task<(bool success, Dictionary<string, string> errors)> UpdatePasswordAsync(string userId, UpdatePasswordRequest request);
        Task<bool> DeleteUserAsync(string userId);
        Task<string> UploadProfileImageAsync(string userId, IFormFile file);
        Task<bool> ConfirmEmailAsync(string email);

    }
}
