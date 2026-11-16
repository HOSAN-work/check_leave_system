using System;
using System.Collections.Generic;

namespace PawPuffPractice.Api.Models.EfModels;

public partial class AdminLeaf
{
    public int Id { get; set; }

    public int AdminId { get; set; }

    public int LeaveType { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public string Reason { get; set; } = null!;

    public sbyte ApprovalStatus { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? RejectionReason { get; set; }

    /// <summary>
    /// 1.09:00-18:00 ; 2.10:00-19:00
    /// </summary>
    public sbyte ClassType { get; set; }

    public virtual User Admin { get; set; } = null!;
}
