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
        private readonly IEmailQueueService _emailQueue;

        public UserService(
            IUserRepository userRepository,
            BlobServiceClient blobServiceClient,
            IEmailQueueService emailQueue)
        {
            _userRepository = userRepository;
            _blobServiceClient = blobServiceClient;
            _emailQueue = emailQueue;
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

        public async Task<(bool, Dictionary<string, string>)> UpdateUserAsync(string userId, UpdateUserRequest request)
        {
            var errors = new Dictionary<string, string>();
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return (false, new() { { "general", "User not found." } });

            // Username
            if (!string.IsNullOrWhiteSpace(request.Username))
            {
                if (request.Username.Length < 3 || request.Username.Length > 20)
                    errors["username"] = "Username must be between 3 and 20 characters.";
                else if (!Regex.IsMatch(request.Username, "^[a-zA-Z0-9_]+$"))
                    errors["username"] = "Only letters, numbers and underscores (_) are allowed.";
                else
                {
                    var other = await _userRepository.GetByUsernameAsync(request.Username);
                    if (other != null && other.Id != user.Id)
                        errors["username"] = "Username is already taken.";
                    else
                        user.UserName = request.Username;
                }
            }

            // Email
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                if (!Regex.IsMatch(request.Email, @"^\S+@\S+\.\S+$"))
                    errors["email"] = "Invalid email format.";
                else
                {
                    var existing = await _userRepository.GetByEmailAsync(request.Email);
                    if (existing != null && existing.Id != user.Id)
                        errors["email"] = "Email is already in use.";
                    else
                    {
                        user.Email = request.Email;
                        user.EmailConfirmed = false;

                        var confirmUrl = $"https://jolly-river-05ee55f03.6.azurestaticapps.net/confirm-new-email?email={Uri.EscapeDataString(request.Email)}";

                        var emailMessage = new EmailMessageDto
                        {
                            To = request.Email,
                            Subject = "Confirm your new email address",
                            Body = $"Click here to confirm your new email: {confirmUrl}"
                        };

                        try
                        {
                            await _emailQueue.SendEmailAsync(emailMessage);
                        }
                        catch (Exception ex)
                        {
                            errors["email"] = "Failed to queue confirmation email: " + ex.Message;
                        }
                    }
                }
            }

            // Password
            if (!string.IsNullOrWhiteSpace(request.NewPassword))
            {
                if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                    errors["currentPassword"] = "Current password is required.";
                else if (!await _userRepository.CheckPasswordAsync(user, request.CurrentPassword))
                    errors["currentPassword"] = "Incorrect current password.";

                if (request.NewPassword.Length < 8 ||
                    !Regex.IsMatch(request.NewPassword, "[A-Z]") ||
                    !Regex.IsMatch(request.NewPassword, "[a-z]") ||
                    !Regex.IsMatch(request.NewPassword, "[0-9]") ||
                    !Regex.IsMatch(request.NewPassword, "[^a-zA-Z0-9]"))
                    errors["newPassword"] = "Password must include uppercase, lowercase, number, and special character.";

                if (request.NewPassword != request.ConfirmPassword)
                    errors["confirmPassword"] = "Password confirmation does not match.";

                if (!errors.Any(e => e.Key.StartsWith("currentPassword") || e.Key.StartsWith("newPassword")))
                    await _userRepository.UpdatePasswordAsync(user, request.NewPassword);
            }

            if (errors.Any()) return (false, errors);

            await _userRepository.UpdateAsync(user);
            return (true, new());
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;
            await _userRepository.DeleteAsync(user);
            return true;
        }

        public async Task<string> UploadProfileImageAsync(string userId, IFormFile file, bool deleteOldImage = false)
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

            if (deleteOldImage && !string.IsNullOrEmpty(user.ProfileImageUrl))
            {
                var uri = new Uri(user.ProfileImageUrl);
                var oldBlobName = Path.GetFileName(uri.LocalPath);
                var oldBlob = container.GetBlobClient(oldBlobName);
                await oldBlob.DeleteIfExistsAsync();
            }

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

        public async Task<bool> ConfirmEmailAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null || user.EmailConfirmed) return false;

            user.EmailConfirmed = true;
            await _userRepository.UpdateAsync(user);

            return true;
        }
    }
}