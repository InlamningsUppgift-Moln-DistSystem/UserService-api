// Repositories/IUserRepository.cs
using UserMicroService.Models;

namespace UserMicroService.Repositories
{
    public interface IUserRepository
    {
        Task<ApplicationUser?> GetByIdAsync(string userId);
        Task UpdateAsync(ApplicationUser user);
        Task DeleteAsync(ApplicationUser user);
    }
}