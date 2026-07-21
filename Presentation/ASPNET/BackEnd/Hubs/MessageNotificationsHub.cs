using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ASPNET.BackEnd.Hubs;

[Authorize(Roles = "Messages")]
public sealed class MessageNotificationsHub : Hub
{
}
