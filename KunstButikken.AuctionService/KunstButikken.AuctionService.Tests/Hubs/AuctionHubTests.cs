using System.Threading;
using System.Threading.Tasks;
using KunstButikken.AuctionService.Hubs;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace KunstButikken.AuctionService.Tests.Hubs;

public class AuctionHubTests
{
    [Fact]
    public async Task JoinAuction_AddsConnectionToGroup_WhenAuctionIdProvided()
    {
        var hub = new AuctionHub();
        var groups = new Mock<IGroupManager>();
        var context = new Mock<HubCallerContext>();
        context.SetupGet(c => c.ConnectionId).Returns("conn-1");
        hub.Context = context.Object;
        hub.Groups = groups.Object;

        await hub.JoinAuction("auction-1");

        groups.Verify(g => g.AddToGroupAsync("conn-1", "auction-1", default), Times.Once);
    }

    [Fact]
    public async Task JoinAuction_DoesNothing_WhenAuctionIdEmpty()
    {
        var hub = new AuctionHub();
        var groups = new Mock<IGroupManager>();
        hub.Groups = groups.Object;
        hub.Context = Mock.Of<HubCallerContext>();

        await hub.JoinAuction(string.Empty);

        groups.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LeaveAuction_RemovesConnectionFromGroup_WhenAuctionIdProvided()
    {
        var hub = new AuctionHub();
        var groups = new Mock<IGroupManager>();
        var context = new Mock<HubCallerContext>();
        context.SetupGet(c => c.ConnectionId).Returns("conn-2");
        hub.Context = context.Object;
        hub.Groups = groups.Object;

        await hub.LeaveAuction("auction-2");

        groups.Verify(g => g.RemoveFromGroupAsync("conn-2", "auction-2", default), Times.Once);
    }

    [Fact]
    public async Task LeaveAuction_DoesNothing_WhenAuctionIdBlank()
    {
        var hub = new AuctionHub();
        var groups = new Mock<IGroupManager>();
        hub.Groups = groups.Object;
        hub.Context = Mock.Of<HubCallerContext>();

        await hub.LeaveAuction(" ");

        groups.Verify(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
