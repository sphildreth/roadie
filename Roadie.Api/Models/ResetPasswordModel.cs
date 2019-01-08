using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Roadie.Api.Models
{
    public class ResetPasswordModel : LoginModel
    {
        [Required]
        public string Token { get; set; }

        [Required]
        [Compare("Password")]
        public String PasswordConfirmation { get; set; }

    }
}
