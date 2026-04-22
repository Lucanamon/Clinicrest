using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace api.Hubs;

[Authorize]
public class NotificationHub : Hub
{
}
