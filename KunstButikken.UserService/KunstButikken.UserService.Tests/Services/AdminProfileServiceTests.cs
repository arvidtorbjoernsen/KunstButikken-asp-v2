using FluentAssertions;
using KunstButikken.UserService.Application.Services;
using KunstButikken.UserService.Domain.Entities;
using KunstButikken.UserService.Domain.Interfaces;
using Moq;
using Xunit;

namespace KunstButikken.UserService.Tests.Services;

public sealed class AdminProfileServiceTests
{
    private readonly Mock<IUserRepository> _repo = new();
    private readonly AdminProfileService _sut;

    public AdminProfileServiceTests()
    {
        _sut = new AdminProfileService(_repo.Object);
    }

    [Fact]
    public async Task GetPendingSellersAsync_ReturnsListFromRepository()
    {
        var pendingSellers = new List<UserProfile>
        {
            new() { Id = Guid.NewGuid(), IsSeller = true, IsSellerVerified = false },
            new() { Id = Guid.NewGuid(), IsSeller = true, IsSellerVerified = false }
        };
        _repo.Setup(r => r.GetPendingSellersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingSellers);

        var result = await _sut.GetPendingSellersAsync();

        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(pendingSellers);
    }

    [Fact]
    public async Task GetPendingSellersAsync_ReturnsEmptyListWhenNoPendingSellers()
    {
        _repo.Setup(r => r.GetPendingSellersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile>());

        var result = await _sut.GetPendingSellersAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAdminsAsync_ReturnsListFromRepository()
    {
        var admins = new List<UserProfile>
        {
            new() { Id = Guid.NewGuid(), IsAdmin = true, DisplayName = "Admin1" },
            new() { Id = Guid.NewGuid(), IsAdmin = true, DisplayName = "Admin2" }
        };
        _repo.Setup(r => r.GetAdminsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(admins);

        var result = await _sut.GetAdminsAsync();

        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(admins);
    }

    [Fact]
    public async Task GetAdminsAsync_ReturnsEmptyListWhenNoAdmins()
    {
        _repo.Setup(r => r.GetAdminsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile>());

        var result = await _sut.GetAdminsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ToggleAdminAsync_MakesUserAdmin()
    {
        var userId = Guid.NewGuid();
        var profile = new UserProfile { UserId = userId, IsAdmin = false };
        _repo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        await _sut.ToggleAdminAsync(userId, true);

        profile.IsAdmin.Should().BeTrue();
        _repo.Verify(r => r.UpdateAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleAdminAsync_RemovesAdminStatus()
    {
        var userId = Guid.NewGuid();
        var profile = new UserProfile { UserId = userId, IsAdmin = true };
        _repo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        await _sut.ToggleAdminAsync(userId, false);

        profile.IsAdmin.Should().BeFalse();
        _repo.Verify(r => r.UpdateAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleAdminAsync_ThrowsWhenProfileNotFound()
    {
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ToggleAdminAsync(userId, true));

        exception.Message.Should().Be("Profile not found");
    }

    [Fact]
    public async Task MakeAdminByEmailAsync_SetsAdminStatusAndReturnsProfile()
    {
        var email = "user@example.com";
        var profile = new UserProfile { Email = email, IsAdmin = false };
        _repo.Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await _sut.MakeAdminByEmailAsync(email);

        result.Should().BeSameAs(profile);
        result.IsAdmin.Should().BeTrue();
        _repo.Verify(r => r.UpdateAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MakeAdminByEmailAsync_ThrowsWhenProfileNotFound()
    {
        var email = "nonexistent@example.com";
        _repo.Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.MakeAdminByEmailAsync(email));

        exception.Message.Should().Be("User profile with this email was not found");
    }

    [Fact]
    public async Task ToggleSellerVerificationAsync_VerifiesSeller()
    {
        var userId = Guid.NewGuid();
        var profile = new UserProfile { UserId = userId, IsSeller = true, IsSellerVerified = false };
        _repo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        await _sut.ToggleSellerVerificationAsync(userId, true);

        profile.IsSellerVerified.Should().BeTrue();
        profile.IsSeller.Should().BeTrue();
        _repo.Verify(r => r.UpdateAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleSellerVerificationAsync_UnverifiesSellerAndRemovesSellerStatus()
    {
        var userId = Guid.NewGuid();
        var profile = new UserProfile { UserId = userId, IsSeller = true, IsSellerVerified = true };
        _repo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        await _sut.ToggleSellerVerificationAsync(userId, false);

        profile.IsSellerVerified.Should().BeFalse();
        profile.IsSeller.Should().BeFalse();
        _repo.Verify(r => r.UpdateAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleSellerVerificationAsync_ThrowsWhenProfileNotFound()
    {
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ToggleSellerVerificationAsync(userId, true));

        exception.Message.Should().Be("Profile not found");
    }

    [Fact]
    public async Task ToggleSellerVerificationAsync_ThrowsWhenVerifyingNonSeller()
    {
        var userId = Guid.NewGuid();
        var profile = new UserProfile { UserId = userId, IsSeller = false };
        _repo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ToggleSellerVerificationAsync(userId, true));

        exception.Message.Should().Be("User is not a seller");
    }

    [Fact]
    public async Task GetPendingSellersAsync_PassesCancellationToken()
    {
        var cts = new CancellationTokenSource();
        _repo.Setup(r => r.GetPendingSellersAsync(cts.Token))
            .ReturnsAsync(new List<UserProfile>());

        await _sut.GetPendingSellersAsync(cts.Token);

        _repo.Verify(r => r.GetPendingSellersAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetAdminsAsync_PassesCancellationToken()
    {
        var cts = new CancellationTokenSource();
        _repo.Setup(r => r.GetAdminsAsync(cts.Token))
            .ReturnsAsync(new List<UserProfile>());

        await _sut.GetAdminsAsync(cts.Token);

        _repo.Verify(r => r.GetAdminsAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task ToggleAdminAsync_PassesCancellationToken()
    {
        var userId = Guid.NewGuid();
        var profile = new UserProfile { UserId = userId };
        var cts = new CancellationTokenSource();
        _repo.Setup(r => r.GetByUserIdAsync(userId, cts.Token))
            .ReturnsAsync(profile);

        await _sut.ToggleAdminAsync(userId, true, cts.Token);

        _repo.Verify(r => r.GetByUserIdAsync(userId, cts.Token), Times.Once);
        _repo.Verify(r => r.UpdateAsync(profile, cts.Token), Times.Once);
    }

    [Fact]
    public async Task MakeAdminByEmailAsync_PassesCancellationToken()
    {
        var email = "test@example.com";
        var profile = new UserProfile { Email = email };
        var cts = new CancellationTokenSource();
        _repo.Setup(r => r.GetByEmailAsync(email, cts.Token))
            .ReturnsAsync(profile);

        await _sut.MakeAdminByEmailAsync(email, cts.Token);

        _repo.Verify(r => r.GetByEmailAsync(email, cts.Token), Times.Once);
        _repo.Verify(r => r.UpdateAsync(profile, cts.Token), Times.Once);
    }

    [Fact]
    public async Task ToggleSellerVerificationAsync_PassesCancellationToken()
    {
        var userId = Guid.NewGuid();
        var profile = new UserProfile { UserId = userId, IsSeller = true };
        var cts = new CancellationTokenSource();
        _repo.Setup(r => r.GetByUserIdAsync(userId, cts.Token))
            .ReturnsAsync(profile);

        await _sut.ToggleSellerVerificationAsync(userId, true, cts.Token);

        _repo.Verify(r => r.GetByUserIdAsync(userId, cts.Token), Times.Once);
        _repo.Verify(r => r.UpdateAsync(profile, cts.Token), Times.Once);
    }

    [Fact]
    public async Task MakeAdminByEmailAsync_DoesNotChangeAlreadyAdmin()
    {
        var email = "admin@example.com";
        var profile = new UserProfile { Email = email, IsAdmin = true };
        _repo.Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await _sut.MakeAdminByEmailAsync(email);

        result.IsAdmin.Should().BeTrue();
        _repo.Verify(r => r.UpdateAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleAdminAsync_HandlesCaseInsensitiveToggle()
    {
        var userId = Guid.NewGuid();
        var profile = new UserProfile { UserId = userId, IsAdmin = false };
        _repo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Toggle on
        await _sut.ToggleAdminAsync(userId, true);
        profile.IsAdmin.Should().BeTrue();

        // Toggle off
        await _sut.ToggleAdminAsync(userId, false);
        profile.IsAdmin.Should().BeFalse();

        _repo.Verify(r => r.UpdateAsync(profile, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}

