using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ASPNET.BackEnd.Hubs;

[Authorize]
public class OrderNotificationsHub : Hub
{
}

