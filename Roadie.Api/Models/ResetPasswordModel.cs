using System.ComponentModel.DataAnnotations;

namespace Roadie.Api.Models
{
    public class ResetPasswordModel : LoginModel
    {
        [Required] [Compare("Password")] public string PasswordConfirmation { get; set; }
        [Required] public string Token { get; set; }
    }
}