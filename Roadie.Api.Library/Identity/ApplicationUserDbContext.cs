using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Roadie.Library.Identity
{
    public class ApplicationUserDbContext : IdentityDbContext<
        User, UserRole, int,
        UserClaims, UsersInRoles, IdentityUserLogin<int>,
        UserRoleClaims, IdentityUserToken<int>>
    {
        public ApplicationUserDbContext(DbContextOptions<ApplicationUserDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // The tables are setup in RoadieDBContext but the table mappings below are necessary for IdentityDbContext to work.

            builder.Entity<User>(entity =>
            {
                entity.ToTable("user");

                // Each User can have many UserClaims
                entity.HasMany(e => e.UserClaims)
                    .WithOne(e => e.User)
                    .HasForeignKey(uc => uc.Id)
                    .IsRequired();

                // Each User can have many entries in the UserRole join table
                entity.HasMany(e => e.UserRoles)
                    .WithOne(e => e.User)
                    .HasForeignKey(ur => ur.UserId)
                    .IsRequired();

            });

            builder.Entity<UserClaims>(entity =>
            {
                entity.ToTable("userClaims");
            });

            builder.Entity<UserRoleClaims>(entity =>
            {
                entity.ToTable("userRoleClaims");
            });

            builder.Entity<UserRole>(entity =>
            {
                entity.ToTable("userrole");
                entity.HasKey(ar => ar.Id);

                // Each Role can have many entries in the UserRole join table
                entity.HasMany(e => e.UserRoles)
                    .WithOne(e => e.Role)
                    .HasForeignKey(ur => ur.RoleId)
                    .IsRequired();

                // Each Role can have many associated RoleClaims
                entity.HasMany(e => e.RoleClaims)
                    .WithOne(e => e.UserRole)
                    .HasForeignKey(rc => rc.RoleId)
                    .IsRequired();

            });

            builder.Entity<UsersInRoles>(entity =>
            {
                entity.ToTable("usersInRoles");
            });            
        }
    }
}