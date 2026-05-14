using System.ComponentModel.DataAnnotations;

namespace Basecamp_Backend.ViewModels.Account
{
    public class RegisterVM
    {
        [MaxLength(55)]
        [MinLength(3)]
        [Required]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(35)]
        [MinLength(4)]
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
