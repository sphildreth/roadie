using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Roadie.Library.Identity
{
    public class ApplicationUserDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
    {
        public DbSet<UsersInRoles> UsersInRoles { get; set; }

        public ApplicationUserDbContext(DbContextOptions<ApplicationUserDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>().ToTable("user");
            modelBuilder.Entity<ApplicationRole>().ToTable("userrole");
        }
    }
}