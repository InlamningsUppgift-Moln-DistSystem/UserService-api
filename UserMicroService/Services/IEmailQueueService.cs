using System.Threading.Tasks;
using UserMicroService.DTOs;

namespace UserMicroService.Services
{
    public interface IEmailQueueService
    {
        Task SendEmailAsync(EmailMessageDto message);
    }
}
