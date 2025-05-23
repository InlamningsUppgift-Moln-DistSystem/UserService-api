// DTOs/UserResponse.cs
namespace UserMicroService.DTOs
{
    public class UserResponse
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? Initials { get; set; }
    }
}