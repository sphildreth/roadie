using Microsoft.AspNetCore.SignalR;
using Roadie.Library.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
