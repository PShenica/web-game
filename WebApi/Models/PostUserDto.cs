﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebApi.Models
{
    public class PostUserDto
    {
        [Required]
        [RegularExpression("[\\w]*", ErrorMessage = "Login should contain only letters or digits")]
        public string Login { get; set; }
        [DefaultValue("John")]
        public string FirstName { get; set; }
        [DefaultValue("Doe")]
        public string LastName { get; set; }
    }
}