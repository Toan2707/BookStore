using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Demo1.Models
{
    public class RegisterViewModel
    {
        public string Username { get; set; }

        public string Password { get; set; }

        [Compare("Password")]
        public string ConfirmPassword { get; set; }

        public string Role { get; set; }

        // Thông tin Customer
        public string FullName { get; set; }

        public string Gender { get; set; }

        public DateTime? Birthday { get; set; }

        public string Phone { get; set; }

        public string Email { get; set; }

        public string Address { get; set; }

        public string Image { get; set; }

        public HttpPostedFileBase ImageFile { get; set; }
    }
}