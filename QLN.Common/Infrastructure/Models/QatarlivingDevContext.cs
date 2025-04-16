using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace QLN.Common.Infrastructure.Models;

public partial class QatarlivingDevContext : DbContext
{
    public QatarlivingDevContext(DbContextOptions<QatarlivingDevContext> options)
        : base(options)
    {
    }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Usertransaction> Usertransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Dateofbirth).HasColumnName("dateofbirth");
            entity.Property(e => e.Emailaddress)
                .HasMaxLength(255)
                .HasColumnName("emailaddress");
            entity.Property(e => e.Firstname)
                .HasMaxLength(100)
                .HasColumnName("firstname");
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .HasColumnName("gender");
            entity.Property(e => e.Isactive).HasColumnName("isactive");
            entity.Property(e => e.Languagepreferences)
                .HasMaxLength(50)
                .HasColumnName("languagepreferences");
            entity.Property(e => e.Lastname)
                .HasMaxLength(100)
                .HasColumnName("lastname");
            entity.Property(e => e.Location)
                .HasMaxLength(255)
                .HasColumnName("location");
            entity.Property(e => e.Mobilenumber)
                .HasMaxLength(15)
                .HasColumnName("mobilenumber");
            entity.Property(e => e.Mobileoperator)
                .HasMaxLength(50)
                .HasColumnName("mobileoperator");
            entity.Property(e => e.Nationality)
                .HasMaxLength(100)
                .HasColumnName("nationality");
            entity.Property(e => e.Password).HasColumnName("password");
            entity.Property(e => e.RefreshToken).HasColumnName("refresh_token");
            entity.Property(e => e.RefreshTokenExpiry).HasColumnName("refresh_token_expiry");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");
        });

        modelBuilder.Entity<Usertransaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("usertransaction_pkey");

            entity.ToTable("usertransaction");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Createdby).HasColumnName("createdby");
            entity.Property(e => e.Createdutc)
                .HasDefaultValueSql("now()")
                .HasColumnName("createdutc");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.Isactive).HasColumnName("isactive");
            entity.Property(e => e.Otp)
                .HasMaxLength(10)
                .HasColumnName("otp");
            entity.Property(e => e.Updatedby).HasColumnName("updatedby");
            entity.Property(e => e.Updatedutc)
                .HasDefaultValueSql("now()")
                .HasColumnName("updatedutc");

            entity.HasOne(d => d.CreatedbyNavigation).WithMany(p => p.UsertransactionCreatedbyNavigations)
                .HasForeignKey(d => d.Createdby)
                .HasConstraintName("fk_createdby");

            entity.HasOne(d => d.UpdatedbyNavigation).WithMany(p => p.UsertransactionUpdatedbyNavigations)
                .HasForeignKey(d => d.Updatedby)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_updatedby");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
