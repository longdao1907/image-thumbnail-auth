using System.ComponentModel.DataAnnotations;

namespace AuthAPI.Core.Application.DTOs
{
    public class RegistrationRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;
    }
}
