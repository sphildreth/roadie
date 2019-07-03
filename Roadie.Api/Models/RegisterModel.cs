using System.ComponentModel.DataAnnotations;

namespace Roadie.Api.Models
{
    public class RegisterModel : LoginModel
    {
        [Required] [EmailAddress] public string Email { get; set; }

        [Required] [Compare("Password")] public string PasswordConfirmation { get; set; }
    }
}