// Services/UserService.cs
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
        public async Task<string> UploadProfileImageAsync(string userId, IFormFile file)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new Exception("User not found");

            // 1. Blob container och namn
            var container = _blobServiceClient.GetBlobContainerClient("profileimages");
            var blobName = $"{userId}-{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            // 2. Ladda upp
            var blobClient = container.GetBlobClient(blobName);
            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });
            }

            // 3. Spara URL i användaren
            user.ProfileImageUrl = blobClient.Uri.ToString();
            await _userRepository.UpdateAsync(user);

            return user.ProfileImageUrl!;
        }
    }
}
