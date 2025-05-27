using Azure.Messaging.ServiceBus;
using System.Text.Json;
using System.Text;
using UserMicroService.DTOs;

namespace UserMicroService.Services
{
    public class EmailQueueService : IEmailQueueService
    {
        private readonly string _connectionString;

        public EmailQueueService(IConfiguration config)
        {
            _connectionString = config["ServiceBus:ConnectionString"]!;
        }

        public async Task SendEmailAsync(EmailMessageDto message)
        {
            await using var client = new ServiceBusClient(_connectionString);
            var sender = client.CreateSender("email-queue");

            var json = JsonSerializer.Serialize(message);
            var busMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(json));

            await sender.SendMessageAsync(busMessage);
        }
    }
}
