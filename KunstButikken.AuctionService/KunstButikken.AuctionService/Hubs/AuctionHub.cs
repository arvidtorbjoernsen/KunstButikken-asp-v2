using Microsoft.AspNetCore.SignalR;

namespace KunstButikken.AuctionService.Hubs;

public class AuctionHub : Hub
{
    public async Task JoinAuction(string auctionId)
    {
        if (!string.IsNullOrWhiteSpace(auctionId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, auctionId).ConfigureAwait(false);
        }
    }

    public async Task LeaveAuction(string auctionId)
    {
        if (!string.IsNullOrWhiteSpace(auctionId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, auctionId).ConfigureAwait(false);
        }
    }
}
