using System.ComponentModel.DataAnnotations;

namespace Roadie.Api.Models
{
    public class LoginModel
    {
        [Required] public string Password { get; set; }

        [Required] public string Username { get; set; }
    }
}
