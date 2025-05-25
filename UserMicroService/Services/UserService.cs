// UserService.cs
using System.Text.RegularExpressions;
using UserMicroService.DTOs;
using UserMicroService.Models;
using UserMicroService.Repositories;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace UserMicroService.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly BlobServiceClient _blobServiceClient;

        public UserService(IUserRepository userRepository, BlobServiceClient blobServiceClient)
        {
            _userRepository = userRepository;
            _blobServiceClient = blobServiceClient;
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

        public async Task<(bool, Dictionary<string, string>)> UpdateUsernameAsync(string userId, string username)
        {
            var errors = new Dictionary<string, string>();
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return (false, new() { { "general", "User not found." } });

            if (string.IsNullOrWhiteSpace(username))
                errors["username"] = "Username is required.";
            else if (username.Length < 3 || username.Length > 20)
                errors["username"] = "Username must be between 3 and 20 characters.";
            else if (!Regex.IsMatch(username, "^[a-zA-Z0-9_]+$"))
                errors["username"] = "Only letters, numbers and underscores (_) are allowed.";
            else
            {
                var otherUser = await _userRepository.GetByUsernameAsync(username);
                if (otherUser != null && otherUser.Id != user.Id)
                    errors["username"] = "Username is already taken.";
                else
                    user.UserName = username;
            }

            if (errors.Any()) return (false, errors);
            await _userRepository.UpdateAsync(user);
            return (true, new());
        }

        public async Task<(bool, Dictionary<string, string>)> UpdateEmailAsync(string userId, string email)
        {
            var errors = new Dictionary<string, string>();
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return (false, new() { { "general", "User not found." } });

            if (string.IsNullOrWhiteSpace(email))
                errors["email"] = "Email is required.";
            else if (!Regex.IsMatch(email, "^\\S+@\\S+\\.\\S+$"))
                errors["email"] = "Invalid email format.";
            else
            {
                var otherUser = await _userRepository.GetByEmailAsync(email);
                if (otherUser != null && otherUser.Id != user.Id)
                    errors["email"] = "Email is already in use.";
                else
                    errors["email"] = "Email change requires confirmation."; // TODO: handle confirmation
            }

            return (errors.Any() ? (false, errors) : (true, new()));
        }

        public async Task<(bool, Dictionary<string, string>)> UpdatePasswordAsync(string userId, UpdatePasswordRequest request)
        {
            var errors = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                errors["currentPassword"] = "Current password is required.";
            if (string.IsNullOrWhiteSpace(request.NewPassword))
                errors["newPassword"] = "New password is required.";
            else if (request.NewPassword.Length < 8 ||
                     !Regex.IsMatch(request.NewPassword, "[A-Z]") ||
                     !Regex.IsMatch(request.NewPassword, "[a-z]") ||
                     !Regex.IsMatch(request.NewPassword, "[0-9]") ||
                     !Regex.IsMatch(request.NewPassword, "[^a-zA-Z0-9]"))
                errors["newPassword"] = "Password must be at least 8 characters and include uppercase, lowercase, number, and special character.";
            if (request.NewPassword != request.ConfirmPassword)
                errors["confirmPassword"] = "Password confirmation does not match.";

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return (false, new() { { "general", "User not found." } });
            if (!await _userRepository.CheckPasswordAsync(user, request.CurrentPassword))
                errors["currentPassword"] = "Incorrect current password.";

            if (errors.Any()) return (false, errors);

            await _userRepository.UpdatePasswordAsync(user, request.NewPassword);
            return (true, new());
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;
            await _userRepository.DeleteAsync(user);
            return true;
        }

        public async Task<string> UploadProfileImageAsync(string userId, IFormFile file)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new Exception("User not found");

            if (file == null || file.Length == 0)
                throw new Exception("No file provided.");

            if (file.Length > 2 * 1024 * 1024)
                throw new Exception("File too large. Max size is 2MB.");

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!new[] { ".jpg", ".jpeg", ".png" }.Contains(extension))
                throw new Exception("Invalid file format. Only .jpg, .jpeg, and .png are allowed.");

            var container = _blobServiceClient.GetBlobContainerClient("profileimages");
            var blobName = $"{userId}-{Guid.NewGuid()}{extension}";
            var blobClient = container.GetBlobClient(blobName);

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });
            }

            user.ProfileImageUrl = blobClient.Uri.ToString();
            await _userRepository.UpdateAsync(user);

            return user.ProfileImageUrl!;
        }
    }
}