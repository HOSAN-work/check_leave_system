using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using PawPuffPractice.Api.Models.EfModels;

namespace PawPuffPractice.Api.Data;

public partial class PawPuffPracticeMySQLDbContext : DbContext
{
    public PawPuffPracticeMySQLDbContext(DbContextOptions<PawPuffPracticeMySQLDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AdminClock> AdminClocks { get; set; }

    public virtual DbSet<AdminLeaf> AdminLeaves { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<AdminClock>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => new { e.AdminId, e.WorkDate }, "uq_admin_clocks_admin_work_date").IsUnique();

            entity.Property(e => e.ClockInTime).HasColumnType("datetime");
            entity.Property(e => e.ClockOutTime).HasColumnType("datetime");

            entity.HasOne(d => d.Admin).WithMany(p => p.AdminClocks)
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_admin_clocks_users_admin_id");
        });

        modelBuilder.Entity<AdminLeaf>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.AdminId, "fk_admin_leave_users_admin_id");

            entity.Property(e => e.ApprovalStatus).HasDefaultValueSql("'1'");
            entity.Property(e => e.ClassType)
                .HasDefaultValueSql("'1'")
                .HasComment("1.09:00-18:00 ; 2.10:00-19:00");
            entity.Property(e => e.DeletedAt).HasColumnType("datetime");
            entity.Property(e => e.EndTime).HasColumnType("datetime");
            entity.Property(e => e.Reason).HasMaxLength(100);
            entity.Property(e => e.RejectionReason).HasMaxLength(500);
            entity.Property(e => e.StartTime).HasColumnType("datetime");

            entity.HasOne(d => d.Admin).WithMany(p => p.AdminLeaves)
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_admin_leave_users_admin_id");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.Account, "uq_users_account").IsUnique();

            entity.HasIndex(e => e.Email, "uq_users_email").IsUnique();

            entity.HasIndex(e => e.Phone, "uq_users_phone").IsUnique();

            entity.Property(e => e.Account).HasMaxLength(20);
            entity.Property(e => e.Biography).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("Created_at");
            entity.Property(e => e.Email).HasMaxLength(320);
            entity.Property(e => e.LastLoginAt)
                .HasColumnType("datetime")
                .HasColumnName("Last_login_at");
            entity.Property(e => e.LineUserId).HasMaxLength(255);
            entity.Property(e => e.Nickname).HasMaxLength(100);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.PointBalance).HasColumnName("Point_balance");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Member'");
            entity.Property(e => e.Status).HasDefaultValueSql("'1'");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
