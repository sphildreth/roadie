using Microsoft.AspNetCore.SignalR;
using Roadie.Library.Models;
using System.Threading.Tasks;

namespace Roadie.Api.Hubs
{
    public class PlayActivityHub : Hub
    {
        public Task SendActivityAsync(PlayActivityList playActivity, System.Threading.CancellationToken cancellationToken)
        {
            return Clients.All.SendAsync("PlayActivity", playActivity, cancellationToken);
        }
    }
}