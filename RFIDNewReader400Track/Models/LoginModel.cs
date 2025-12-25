using System.ComponentModel.DataAnnotations;

namespace RFIDReaderPortal.Models
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Username is required")]
        public string? Email { get; set; }
        public string? Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public string? SessionId { get; set; }
        public string? IpAddress { get; set; }
        public string? Statusmessage { get; set; }

        public string? DeviceId { get; set; }
    }
}