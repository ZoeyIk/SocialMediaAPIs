using System.ComponentModel.DataAnnotations;

namespace SocialAPI.Model
{
    public class UserComment
    {
        [Key]
        [Required]
        public int CommentID { get; set; }

        [Required]
        public int PostID { get; set; }

        [Required]
        public int UserID { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }

        [Required]
        public DateTime CreatedTime { get; set; }

        [StringLength(200)]
        public string FullName { get; set; }
    }
}
