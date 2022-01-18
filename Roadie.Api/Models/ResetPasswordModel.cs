using System.ComponentModel.DataAnnotations;

namespace Roadie.Api.Models
{
    public class ResetPasswordModel : LoginModel
    {
        [Required]
        [Compare(nameof(Password))]
        public string PasswordConfirmation { get; set; }

        [Required]
        public string Token { get; set; }
    }
}
