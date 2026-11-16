using System;
using System.Collections.Generic;

namespace PawPuffPractice.Api.Models.EfModels;

public partial class AdminClock
{
    public int Id { get; set; }

    public int AdminId { get; set; }

    public DateOnly WorkDate { get; set; }

    public DateTime ClockInTime { get; set; }

    public DateTime? ClockOutTime { get; set; }

    public virtual User Admin { get; set; } = null!;
}
