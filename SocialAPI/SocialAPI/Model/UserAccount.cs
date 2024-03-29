using System.ComponentModel.DataAnnotations;

namespace SocialAPI.Model
{
    public class UserAccount
    {
        [Key]
        [Required]
        public int UserID { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }

        [StringLength(200, ErrorMessage = "The {0} cannot exceed {1} characters. ")]
        public string? FullName { get; set; }
        
    }
}
