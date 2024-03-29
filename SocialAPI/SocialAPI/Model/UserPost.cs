using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace SocialAPI.Model
{
    public class UserPost
    {
        [Key]
        [Required]
        public int PostID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public DateTime CreatedTime { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; }

        public Byte[]? Image { get; set; }

        public int Likes { get; set; }

        [StringLength(200)]
        public string? ImageName { get; set; }

        [StringLength(200)]
        public string FullName { get; set; }

        // Comment class
        public List<UserComment>? Comments { get; set; }
    }
}
