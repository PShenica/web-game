using System.ComponentModel.DataAnnotations;

namespace WebApi.Models
{
    public class PatchDto
    {
        [Required]
        [RegularExpression("[\\w]*", ErrorMessage = "Login should contain only letters or digits")]
        public string Login { get; set; }
        [Required(ErrorMessage = "FirstName is missing")]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "LastName is missing")]
        public string LastName { get; set; }
    }
}