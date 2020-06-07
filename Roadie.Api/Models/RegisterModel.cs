using System;
using System.ComponentModel.DataAnnotations;

namespace Roadie.Api.Models
{
    public class RegisterModel : LoginModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public Guid? InviteToken { get; set; }

        [Required]
        [Compare(nameof(Password))]
        public string PasswordConfirmation { get; set; }
    }
}