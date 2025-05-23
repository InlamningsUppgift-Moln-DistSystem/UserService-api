// Services/UserService.cs
using UserMicroService.DTOs;
using UserMicroService.Models;
using UserMicroService.Repositories;

namespace UserMicroService.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UserResponse?> GetUserAsync(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return null;

            return new UserResponse
            {
                Id = user.Id,
                Username = user.UserName!,
                Email = user.Email!,
                ProfileImageUrl = user.ProfileImageUrl,
                Initials = user.Initials
            };
        }

        public async Task<bool> UpdateUserAsync(string userId, UpdateUserRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            if (!string.IsNullOrWhiteSpace(request.Username))
                user.UserName = request.Username;

            if (!string.IsNullOrWhiteSpace(request.Email))
                user.Email = request.Email;

            if (!string.IsNullOrWhiteSpace(request.ProfileImageUrl))
                user.ProfileImageUrl = request.ProfileImageUrl;

            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            await _userRepository.DeleteAsync(user);
            return true;
        }
    }
}
