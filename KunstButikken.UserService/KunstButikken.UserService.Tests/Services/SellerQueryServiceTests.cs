using FluentAssertions;
using KunstButikken.UserService.Application.Services;
using KunstButikken.UserService.Domain.Dtos;
using KunstButikken.UserService.Domain.Interfaces;
using Moq;
using Xunit;

namespace KunstButikken.UserService.Tests.Services;

public sealed class SellerQueryServiceTests
{
    private readonly Mock<IUserRepository> _repo = new();
    private readonly SellerQueryService _sut;

    public SellerQueryServiceTests()
    {
        _sut = new SellerQueryService(_repo.Object);
    }

    [Fact]
    public async Task GetSellersAsync_ReturnsOrderedSellers()
    {
        var sellers = new List<SellerDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                DisplayName = "B",
                Email = "b@example.com"
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                DisplayName = "A",
                Email = "a@example.com"
            }
        };
        _repo.Setup(r => r.GetSellersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sellers);

        var result = await _sut.GetSellersAsync();

        result.Should().HaveCount(2);
        result.First().DisplayName.Should().Be("B");
    }

    [Fact]
    public async Task GetSellersAsync_ReturnsEmptyListWhenNoSellers()
    {
        _repo.Setup(r => r.GetSellersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<SellerDto>());

        var result = await _sut.GetSellersAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSellersAsync_CallsRepositoryOnce()
    {
        _repo.Setup(r => r.GetSellersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<SellerDto>());

        await _sut.GetSellersAsync();

        _repo.Verify(r => r.GetSellersAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSellerByUserIdAsync_ReturnsSellerWhenFound()
    {
        var userId = Guid.NewGuid();
        var seller = new SellerDto
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DisplayName = "Test Seller",
            Email = "seller@example.com"
        };
        _repo.Setup(r => r.GetSellerByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(seller);

        var result = await _sut.GetSellerByUserIdAsync(userId);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.DisplayName.Should().Be("Test Seller");
    }

    [Fact]
    public async Task GetSellerByUserIdAsync_ReturnsNullWhenNotFound()
    {
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.GetSellerByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((SellerDto?)null);

        var result = await _sut.GetSellerByUserIdAsync(userId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSellerByUserIdAsync_CallsRepositoryWithCorrectUserId()
    {
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.GetSellerByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((SellerDto?)null);

        await _sut.GetSellerByUserIdAsync(userId);

        _repo.Verify(r => r.GetSellerByUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSellersAsync_PassesCancellationToken()
    {
        var cts = new CancellationTokenSource();
        _repo.Setup(r => r.GetSellersAsync(cts.Token)).ReturnsAsync(new List<SellerDto>());

        await _sut.GetSellersAsync(cts.Token);

        _repo.Verify(r => r.GetSellersAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetSellerByUserIdAsync_PassesCancellationToken()
    {
        var userId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        _repo.Setup(r => r.GetSellerByUserIdAsync(userId, cts.Token)).ReturnsAsync((SellerDto?)null);

        await _sut.GetSellerByUserIdAsync(userId, cts.Token);

        _repo.Verify(r => r.GetSellerByUserIdAsync(userId, cts.Token), Times.Once);
    }
}
