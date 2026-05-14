using System.ComponentModel.DataAnnotations;

namespace Basecamp_Backend.ViewModels.Account
{
    public class DeleteAccountVM
    {
        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
