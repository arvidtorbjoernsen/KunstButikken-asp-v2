using FluentAssertions;
using KunstButikken.UserService.Application.Services;
using KunstButikken.UserService.Domain.Entities;
using KunstButikken.UserService.Domain.Interfaces;
using Moq;

namespace KunstButikken.UserService.Tests.Services;

public sealed class SellerQueryServiceTests
{
    [Fact]
    public async Task GetSellersAsync_ReturnsOrderedSellers()
    {
        var users = new List<UserProfile>
        {
            new() { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), DisplayName = "B", Email = "b@example.com", IsSeller = true, CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10) },
            new() { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), DisplayName = "A", Email = "a@example.com", IsSeller = true, CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-20) }
        };
        var queryable = users.AsQueryable();
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.Query()).Returns(queryable);

        var sut = new SellerQueryService(repo.Object);

        var result = await sut.GetSellersAsync();

        result.Should().HaveCount(2);
        result.First().DisplayName.Should().Be("A");
    }
}
