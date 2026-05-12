using System.ComponentModel.DataAnnotations;

namespace Basecamp_Backend.ViewModels.Account
{
    public class RegisterVM
    {
        [MaxLength(55)]
        [MinLength(3)]
        [Required]
        public string FullName { get; set; }
        
        [MaxLength(35)]
        [MinLength(4)]
        [Required]
        public string Username { get; set; }
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [DataType(DataType.Password)]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; }
    }
}
