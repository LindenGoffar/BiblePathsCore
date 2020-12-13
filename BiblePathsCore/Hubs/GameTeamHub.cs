using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace BiblePathsCore.Hubs
{
    public class GameTeamHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            // Was this called? 
            await base.OnConnectedAsync();
        }
        public async Task JoinGroup(string TeamIdString)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, TeamIdString);
        }
        public async Task LeaveGroup(string TeamIdString)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, TeamIdString);
        }
        public async Task BoardStateChange(string TeamIdString)
        {
            await Clients.Group(TeamIdString).SendAsync("StateChange");
        }
    }
}
