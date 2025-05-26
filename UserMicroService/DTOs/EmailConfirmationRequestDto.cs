namespace UserMicroService.DTOs
{
    // 📁 Email/DTOs/EmailConfirmationRequestDto.cs
    public class EmailConfirmationRequestDto
    {
        public string To { get; set; }
        public string ConfirmationUrl { get; set; }
    }

}
