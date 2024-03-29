using Microsoft.EntityFrameworkCore;
using SocialAPI.Model;

namespace SocialAPI.Data
{
    public class UserContext : DbContext
    {
        public UserContext(DbContextOptions<UserContext> options) : base(options)
        {
        }

        public DbSet<UserAccount> UserAccounts { get; set; }
        public DbSet<UserPost> UserPosts { get; set; }
        public DbSet<UserComment> UserComments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserAccount>().ToTable("TBL_USER_ACCOUNT");
            modelBuilder.Entity<UserPost>().ToTable("TBL_USER_POST");
            modelBuilder.Entity<UserComment>().ToTable("TBL_USER_COMMENT");
        }
    }
}
