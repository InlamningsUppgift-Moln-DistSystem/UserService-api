// Services/IUserService.cs
using UserMicroService.DTOs;

namespace UserMicroService.Services
{
    public interface IUserService
    {
        Task<UserResponse?> GetUserAsync(string userId);
        Task<bool> UpdateUserAsync(string userId, UpdateUserRequest request);
        Task<bool> DeleteUserAsync(string userId);
        Task<string> UploadProfileImageAsync(string userId, IFormFile file);

    }
}