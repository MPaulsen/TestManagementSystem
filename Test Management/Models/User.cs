using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Test_Management.Models
{
    public class User
    {
        [Required(ErrorMessage="You must enter a username. (Usually your NID)", AllowEmptyStrings=false)]
        public string Username { get; set; }

        [Required(ErrorMessage="You must enter a password. (Your NID password)", AllowEmptyStrings=false)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public User()
        {

        }

        public User(string user, string pass)
        {
            Username = user;
            Password = pass;
        }
    }
}