using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ESS.Api.Database.Entities.Users;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasMaxLength(500);

        builder.Property(u => u.Name).HasMaxLength(100);

        builder.Property(u => u.NationalCode).IsRequired().HasMaxLength(10);
        builder.HasIndex(u => u.NationalCode).IsUnique();

        builder.Property(u => u.PhoneNumber).IsRequired().HasMaxLength(11);
        builder.HasIndex(u => u.PhoneNumber).IsUnique();

        builder.Property(u => u.PersonalCode).IsRequired().HasMaxLength(6);
        builder.HasIndex(u => u.PersonalCode).IsUnique();

        builder.Property(u => u.CreatedAt).IsRequired();

        builder.Property(u => u.AvatarKey)
                .HasMaxLength(500);

        builder.HasIndex(u => u.IdentityId).IsUnique();
    }
}
