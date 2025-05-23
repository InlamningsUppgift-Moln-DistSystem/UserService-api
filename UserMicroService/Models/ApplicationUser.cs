// Models/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;

namespace UserMicroService.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? ProfileImageUrl { get; set; }
        public string? Initials { get; set; }
    }
}