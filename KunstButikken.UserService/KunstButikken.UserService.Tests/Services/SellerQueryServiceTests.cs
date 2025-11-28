using FluentAssertions;
using KunstButikken.UserService.Application.Services;
using KunstButikken.UserService.Domain.Dtos;
using KunstButikken.UserService.Domain.Interfaces;
using Moq;

namespace KunstButikken.UserService.Tests.Services;

public sealed class SellerQueryServiceTests
{
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
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetSellersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sellers);

        var sut = new SellerQueryService(repo.Object);

        var result = await sut.GetSellersAsync();

        result.Should().HaveCount(2);
        result.First().DisplayName.Should().Be("B");
    }
}
