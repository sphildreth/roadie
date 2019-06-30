using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Roadie.Api.Hubs
{
    public class ScanActivityHub : Hub
    {
        public async Task SendSystemActivity(string scanActivity)
        {
            await Clients.All.SendAsync("SendSystemActivity", scanActivity);
        }
    }
}