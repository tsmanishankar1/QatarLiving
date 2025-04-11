using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace QLN.Backend.API.Models;

public partial class QatarlivingContext : DbContext
{
    public QatarlivingContext()
    {
    }

    public QatarlivingContext(DbContextOptions<QatarlivingContext> options)
        : base(options)
    {
    }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Usertransaction> Usertransactions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=qatarliving;Username=postgres;Password=Password@1");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("userprofile_pkey");

            entity.ToTable("user");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("nextval('userprofile_id_seq'::regclass)")
                .HasColumnName("id");
            entity.Property(e => e.Confirmpassword).HasColumnName("confirmpassword");
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
            entity.Property(e => e.Nationality)
                .HasMaxLength(100)
                .HasColumnName("nationality");
            entity.Property(e => e.Password).HasColumnName("password");
        });

        modelBuilder.Entity<Usertransaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("otplogins_pkey");

            entity.ToTable("usertransaction");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("nextval('otplogins_id_seq'::regclass)")
                .HasColumnName("id");
            entity.Property(e => e.Createdby).HasColumnName("createdby");
            entity.Property(e => e.Createdutc).HasColumnName("createdutc");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.Isactive).HasColumnName("isactive");
            entity.Property(e => e.Otp)
                .HasMaxLength(10)
                .HasColumnName("otp");
            entity.Property(e => e.Updatedby).HasColumnName("updatedby");
            entity.Property(e => e.Updatedutc).HasColumnName("updatedutc");

            entity.HasOne(d => d.CreatedbyNavigation).WithMany(p => p.UsertransactionCreatedbyNavigations)
                .HasForeignKey(d => d.Createdby)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_createdby");

            entity.HasOne(d => d.UpdatedbyNavigation).WithMany(p => p.UsertransactionUpdatedbyNavigations)
                .HasForeignKey(d => d.Updatedby)
                .HasConstraintName("fk_updatedby");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
