using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using KunstButikken.AuctionService.Application.Interfaces;
using KunstButikken.AuctionService.Controllers;
using KunstButikken.AuctionService.Domain.Models;
using KunstButikken.AuctionService.Hubs;
using KunstButikken.AuctionService.Tests.Fixtures;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace KunstButikken.AuctionService.Tests.Controllers;

public class AuctionsControllerTests
{
    private static AuctionsController BuildController(Mock<IAuctionService> service, out Mock<IClientProxy> groupProxy, out Mock<IClientProxy> allProxy)
    {
        var hub = new Mock<IHubContext<AuctionHub>>();
        var clients = new Mock<IHubClients>();
        groupProxy = new Mock<IClientProxy>();
        allProxy = new Mock<IClientProxy>();
        clients.Setup(c => c.All).Returns(allProxy.Object);
        clients.Setup(c => c.Group(It.IsAny<string>())).Returns(groupProxy.Object);
        hub.SetupGet(h => h.Clients).Returns(clients.Object);
        groupProxy.Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        allProxy.Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new AuctionsController(service.Object, hub.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return controller;
    }

    private static void SetUser(AuctionsController controller, string? subject)
    {
        controller.ControllerContext ??= new ControllerContext();
        controller.ControllerContext.HttpContext ??= new DefaultHttpContext();
        controller.ControllerContext.HttpContext.User = subject is null
            ? new ClaimsPrincipal()
            : new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", subject) }, "test"));
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithAuctions()
    {
        var service = new Mock<IAuctionService>();
        var controller = BuildController(service, out _, out _);
        var auctions = new List<Auction> { AuctionFixture.CreateAuction() };
        service.Setup(s => s.GetAllAsync(null, true, It.IsAny<CancellationToken>())).ReturnsAsync(auctions);

        var result = await controller.GetAll(null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        ok.Value.Should().BeEquivalentTo(auctions);
    }

    [Fact]
    public async Task Get_ReturnsOk_WhenAuctionExists()
    {
        var service = new Mock<IAuctionService>();
        var controller = BuildController(service, out _, out _);
        var auction = AuctionFixture.CreateAuction();
        service.Setup(s => s.GetByIdAsync(auction.Id, true, It.IsAny<CancellationToken>())).ReturnsAsync(auction);

        var result = await controller.Get(auction.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        ok.Value.Should().BeSameAs(auction);
    }

    [Fact]
    public async Task Get_ReturnsNotFound_WhenMissing()
    {
        var service = new Mock<IAuctionService>();
        var controller = BuildController(service, out _, out _);
        service.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>())).ReturnsAsync((Auction?)null);

        var result = await controller.Get(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAndNotifiesHub()
    {
        var service = new Mock<IAuctionService>();
        var controller = BuildController(service, out var groupProxy, out var allProxy);
        var auction = AuctionFixture.CreateAuction();
        service.Setup(s => s.CreateAsync(auction, It.IsAny<CancellationToken>())).ReturnsAsync(auction);

        var result = await controller.Create(auction);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(AuctionsController.Get), created.ActionName);
        created.Value.Should().BeSameAs(auction);
        allProxy.Verify(p => p.SendCoreAsync("auctionCreated", It.Is<object?[]>(args => AuctionMatches(auction, args)), It.IsAny<CancellationToken>()), Times.Once);
        groupProxy.Verify(p => p.SendCoreAsync("auctionUpdated", It.Is<object?[]>(args => AuctionMatches(auction, args)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_ThrowsArgumentNullException_WhenAuctionIsNull()
    {
        var service = new Mock<IAuctionService>();
        var controller = BuildController(service, out _, out _);

        await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.Create(null!));
    }

    [Fact]
    public async Task Update_ReturnsOk_WhenSuccessful()
    {
        var service = new Mock<IAuctionService>();
        var controller = BuildController(service, out var groupProxy, out _);
        var auctionId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var auction = AuctionFixture.CreateAuction(auctionId);
        auction.SellerId = sellerId;
        var updatedAuction = new Auction
        {
            StartsAt = auction.StartsAt,
            EndsAt = auction.EndsAt,
            StartingPrice = auction.StartingPrice,
            ReservePrice = auction.ReservePrice
        };

        service.Setup(s => s.UpdateAsync(auctionId, updatedAuction, sellerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auction);
        SetUser(controller, sellerId.ToString());

        var result = await controller.Update(auctionId, updatedAuction);

        var ok = Assert.IsType<OkObjectResult>(result);
        ok.Value.Should().BeSameAs(auction);
        groupProxy.Verify(p => p.SendCoreAsync("auctionUpdated", It.Is<object?[]>(args => AuctionMatches(auction, args)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenAuctionMissing()
    {
        var service = new Mock<IAuctionService>();
        var controller = BuildController(service, out _, out _);
        service.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Auction>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Auction?)null);

        var result = await controller.Update(Guid.NewGuid(), AuctionFixture.CreateAuction());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsForbid_WhenUnauthorized()
    {
        var service = new Mock<IAuctionService>();
        var controller = BuildController(service, out _, out _);
        service.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Auction>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException());

        var result = await controller.Update(Guid.NewGuid(), AuctionFixture.CreateAuction());

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsBadRequest_WhenInvalidOperation()
    {
        var service = new Mock<IAuctionService>();
        var controller = BuildController(service, out _, out _);
        var message = "Invalid";
        service.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Auction>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(message));

        var result = await controller.Update(Guid.NewGuid(), AuctionFixture.CreateAuction());

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        badRequest.Value.Should().Be(message);
    }

    [Fact]
    public async Task Bid_ReturnsAcceptedAndNotifies_WhenValid()
    {
        var service = new Mock<IAuctionService>();
        var controller = BuildController(service, out var groupProxy, out _);
        var auction = AuctionFixture.CreateAuction();
        service.Setup(s => s.PlaceBidAsync(auction.Id, 200, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(auction);

        var result = await controller.Bid(auction.Id, 200);

        Assert.IsType<AcceptedResult>(result);
        groupProxy.Verify(p => p.SendCoreAsync("auctionUpdated", It.Is<object?[]>(args => AuctionMatches(auction, args)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Bid_ReturnsNotFound_WhenAuctionMissing()
    {
        var service = new Mock<IAuctionService>();
        var controller = BuildController(service, out _, out _);
        service.Setup(s => s.PlaceBidAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Auction?)null);

        var result = await controller.Bid(Guid.NewGuid(), 100);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Bid_ReturnsBadRequest_WhenInvalidOperation()
    {
        var service = new Mock<IAuctionService>();
        var controller = BuildController(service, out _, out _);
        service.Setup(s => s.PlaceBidAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("bad"));

        var result = await controller.Bid(Guid.NewGuid(), 100);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Close_ReturnsNoContentAndNotifies_WhenSuccess()
    {
        var service = new Mock<IAuctionService>();
        var controller = BuildController(service, out var groupProxy, out _);
        var auction = AuctionFixture.CreateAuction();
        service.Setup(s => s.CloseAsync(auction.Id, It.IsAny<CancellationToken>())).ReturnsAsync(auction);

        var result = await controller.Close(auction.Id);

        Assert.IsType<NoContentResult>(result);
        groupProxy.Verify(p => p.SendCoreAsync("auctionClosed", It.Is<object?[]>(args => AuctionMatches(auction, args)), It.IsAny<CancellationToken>()), Times.Once);
        groupProxy.Verify(p => p.SendCoreAsync("auctionUpdated", It.Is<object?[]>(args => AuctionMatches(auction, args)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Close_ReturnsNotFound_WhenAuctionMissing()
    {
        var service = new Mock<IAuctionService>();
        var controller = BuildController(service, out _, out _);
        service.Setup(s => s.CloseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Auction?)null);

        var result = await controller.Close(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    private static bool AuctionMatches(Auction auction, object?[] args)
        => args.Length == 1 && ReferenceEquals(args[0], auction);
}
