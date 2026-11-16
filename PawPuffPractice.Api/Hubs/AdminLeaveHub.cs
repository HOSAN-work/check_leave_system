using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PawPuffPractice.Api.Hubs;

// : Hub 取得 SignalR 的連線功能；[Authorize] 限制管理員才能連線
[Authorize(Roles = "Admin")]
public class AdminLeaveHub : Hub { }
