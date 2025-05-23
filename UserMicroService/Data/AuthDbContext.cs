using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UserMicroService.Models;

namespace UserMicroService.Data
{
    public class AuthDbContext : IdentityDbContext<ApplicationUser>
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options)
            : base(options)
        {
        }

        // Lägg till om du senare vill ha egna DbSet
        // public DbSet<NågotAnnat> NågotAnnat { get; set; }
    }
}
