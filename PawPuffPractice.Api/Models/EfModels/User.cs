using System;
using System.Collections.Generic;

namespace PawPuffPractice.Api.Models.EfModels;

public partial class User
{
    public int Id { get; set; }

    public string Account { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Nickname { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public int PointBalance { get; set; }

    public sbyte Status { get; set; }

    public string Role { get; set; } = null!;

    public string? Biography { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public string? LineUserId { get; set; }

    public virtual ICollection<AdminClock> AdminClocks { get; set; } = new List<AdminClock>();

    public virtual ICollection<AdminLeaf> AdminLeaves { get; set; } = new List<AdminLeaf>();
}
