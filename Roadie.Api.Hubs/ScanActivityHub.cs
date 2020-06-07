using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Roadie.Api.Hubs
{
    public class ScanActivityHub : Hub
    {
        public Task SendSystemActivityAsync(string scanActivity, System.Threading.CancellationToken cancellationToken)
        {
            return Clients.All.SendAsync("SendSystemActivity", scanActivity, cancellationToken);
        }
    }
}