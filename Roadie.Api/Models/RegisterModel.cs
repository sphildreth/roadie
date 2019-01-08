using System;
using System.ComponentModel.DataAnnotations;

namespace Roadie.Api.Models
{
    public class RegisterModel : LoginModel
    {
        [Required]
        [EmailAddress]
        public String Email { get; set; }    

        [Required]
        [Compare("Password")]
        public String PasswordConfirmation { get; set; }
    }
}