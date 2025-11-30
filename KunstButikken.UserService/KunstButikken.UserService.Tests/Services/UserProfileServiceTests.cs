using System.Security.Claims;

using KunstButikken.ServiceDefaults;
using KunstButikken.UserService.Application.Services;
using KunstButikken.UserService.Domain.Entities;
using KunstButikken.UserService.Domain.Interfaces;

using Moq;

using Xunit;

namespace KunstButikken.UserService.Tests.Services;

public class UserProfileServiceTests
{
    private readonly Mock<IUserRepository> _repo = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly UserProfileService _service;

    public UserProfileServiceTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.Parse("2025-11-30T00:00:00Z"));
        _service = new UserProfileService(_repo.Object, _clock.Object);
    }

    [Fact]
    public async Task GetOrCreateProfileAsync_ExistingProfile_SynchronizesRoles()
    {
        var userId = Guid.NewGuid();
        var existing = new UserProfile { UserId = userId, IsSeller = false, IsAdmin = false };
        _repo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(existing);

        var principal = BuildPrincipal(addRoles: true);
        var result = await _service.GetOrCreateProfileAsync(userId, principal);

        Assert.Same(existing, result);
        Assert.True(result.IsSeller);
        Assert.True(result.IsAdmin);
        _repo.Verify(r => r.UpdateAsync(result, default), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateProfileAsync_MissingProfile_CreatesFromClaims()
    {
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync((UserProfile?)null);

        var principal = BuildPrincipal(addRoles: false);
        var result = await _service.GetOrCreateProfileAsync(userId, principal);

        Assert.Equal(principal.FindFirst(ClaimTypes.Name)?.Value, result.DisplayName);
        Assert.Equal(principal.FindFirst(ClaimTypes.Email)?.Value, result.Email);
        Assert.False(result.IsSeller);
        Assert.False(result.IsAdmin);
        _repo.Verify(r => r.AddAsync(It.Is<UserProfile>(p => p.UserId == userId), default), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_UpdatesAllFieldsCorrectly()
    {
        var userId = Guid.NewGuid();
        var existing = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DisplayName = "Old Name"
        };
        _repo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(existing);

        var updated = new UserProfile
        {
            DisplayName = "New Name",
            Email = "new@example.com",
            FullName = "New Full Name",
            PhoneNumber = "123456789",
            Address = "123 Main St",
            City = "Oslo",
            PostalCode = "0123",
            Country = "Norway",
            ProfileImageUrl = "https://example.com/image.jpg",
            PreferencesJson = "{\"theme\":\"dark\"}"
        };

        var result = await _service.UpdateProfileAsync(userId, updated);

        Assert.Equal("New Name", result.DisplayName);
        Assert.Equal("new@example.com", result.Email);
        Assert.Equal("New Full Name", result.FullName);
        Assert.Equal("123456789", result.PhoneNumber);
        Assert.Equal("123 Main St", result.Address);
        Assert.Equal("Oslo", result.City);
        Assert.Equal("0123", result.PostalCode);
        Assert.Equal("Norway", result.Country);
        Assert.Equal("https://example.com/image.jpg", result.ProfileImageUrl);
        Assert.Equal("{\"theme\":\"dark\"}", result.PreferencesJson);
        Assert.Equal(DateTimeOffset.Parse("2025-11-30T00:00:00Z"), result.UpdatedAt);
        _repo.Verify(r => r.UpdateAsync(existing, default), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_ThrowsWhenProfileNotFound()
    {
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync((UserProfile?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateProfileAsync(userId, new UserProfile()));
    }

    [Fact]
    public async Task RegisterAsync_CreatesNewProfileWhenNotExists()
    {
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync((UserProfile?)null);

        var request = new KunstButikken.UserService.Domain.Dtos.RegistrationRequest
        {
            Email = "test@example.com",
            FullName = "Test User",
            DisplayName = "testuser",
            UserType = "buyer"
        };

        var result = await _service.RegisterAsync(userId, request);

        Assert.Equal(userId, result.UserId);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("Test User", result.FullName);
        Assert.Equal("testuser", result.DisplayName);
        Assert.False(result.IsSeller);
        _repo.Verify(r => r.AddAsync(It.Is<UserProfile>(p => p.UserId == userId), default), Times.Once);
        _repo.Verify(r => r.UpdateAsync(It.IsAny<UserProfile>(), default), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_UpdatesExistingProfile()
    {
        var userId = Guid.NewGuid();
        var existing = new UserProfile { Id = Guid.NewGuid(), UserId = userId };
        _repo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(existing);

        var request = new KunstButikken.UserService.Domain.Dtos.RegistrationRequest
        {
            Email = "updated@example.com",
            FullName = "Updated User",
            DisplayName = "updated",
            UserType = "seller"
        };

        var result = await _service.RegisterAsync(userId, request);

        Assert.Same(existing, result);
        Assert.Equal("updated@example.com", result.Email);
        Assert.True(result.IsSeller);
        Assert.False(result.IsSellerVerified);
        _repo.Verify(r => r.AddAsync(It.IsAny<UserProfile>(), default), Times.Never);
        _repo.Verify(r => r.UpdateAsync(existing, default), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_SetSellerFlagWhenUserTypeisSeller()
    {
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync((UserProfile?)null);

        var request = new KunstButikken.UserService.Domain.Dtos.RegistrationRequest
        {
            UserType = "seller"
        };

        var result = await _service.RegisterAsync(userId, request);

        Assert.True(result.IsSeller);
        Assert.False(result.IsSellerVerified);
    }

    [Fact]
    public async Task RegisterAsync_HandlesCaseInsensitiveUserType()
    {
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync((UserProfile?)null);

        var request = new KunstButikken.UserService.Domain.Dtos.RegistrationRequest
        {
            UserType = "SELLER"
        };

        var result = await _service.RegisterAsync(userId, request);

        Assert.True(result.IsSeller);
    }

    [Fact]
    public async Task VerifySellerAsync_SetsVerificationStatus()
    {
        var profileId = Guid.NewGuid();
        var profile = new UserProfile { Id = profileId, IsSeller = true, IsSellerVerified = false };
        _repo.Setup(r => r.FindAsync(profileId, default)).ReturnsAsync(profile);

        await _service.VerifySellerAsync(profileId, true);

        Assert.True(profile.IsSellerVerified);
        _repo.Verify(r => r.UpdateAsync(profile, default), Times.Once);
    }

    [Fact]
    public async Task VerifySellerAsync_ThrowsWhenProfileNotFound()
    {
        var profileId = Guid.NewGuid();
        _repo.Setup(r => r.FindAsync(profileId, default)).ReturnsAsync((UserProfile?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.VerifySellerAsync(profileId, true));
    }

    [Fact]
    public async Task VerifySellerAsync_ThrowsWhenUserIsNotSeller()
    {
        var profileId = Guid.NewGuid();
        var profile = new UserProfile { Id = profileId, IsSeller = false };
        _repo.Setup(r => r.FindAsync(profileId, default)).ReturnsAsync(profile);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.VerifySellerAsync(profileId, true));
    }

    [Fact]
    public async Task VerifySellerAsync_CanUnverify()
    {
        var profileId = Guid.NewGuid();
        var profile = new UserProfile { Id = profileId, IsSeller = true, IsSellerVerified = true };
        _repo.Setup(r => r.FindAsync(profileId, default)).ReturnsAsync(profile);

        await _service.VerifySellerAsync(profileId, false);

        Assert.False(profile.IsSellerVerified);
    }

    [Fact]
    public async Task ListUnverifiedSellersAsync_ReturnsListFromRepository()
    {
        var unverifiedSellers = new List<KunstButikken.UserService.Domain.Dtos.UnverifiedSellerDto>
        {
            new() { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), DisplayName = "Seller1", Email = "seller1@test.com" },
            new() { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), DisplayName = "Seller2", Email = "seller2@test.com" }
        };
        _repo.Setup(r => r.ListUnverifiedSellersAsync(default)).ReturnsAsync(unverifiedSellers);

        var result = await _service.ListUnverifiedSellersAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(unverifiedSellers, result);
    }

    [Fact]
    public async Task GetOrCreateProfileAsync_NoRoleChanges_DoesNotUpdate()
    {
        var userId = Guid.NewGuid();
        var existing = new UserProfile { UserId = userId, IsSeller = true, IsAdmin = true };
        _repo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(existing);

        var principal = BuildPrincipal(addRoles: true);
        var result = await _service.GetOrCreateProfileAsync(userId, principal);

        _repo.Verify(r => r.UpdateAsync(It.IsAny<UserProfile>(), default), Times.Never);
    }

    [Fact]
    public async Task GetOrCreateProfileAsync_RoleChanged_ResetsSellerVerification()
    {
        var userId = Guid.NewGuid();
        var existing = new UserProfile { UserId = userId, IsSeller = false, IsSellerVerified = true };
        _repo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(existing);

        var principal = BuildPrincipal(addRoles: true);
        await _service.GetOrCreateProfileAsync(userId, principal);

        Assert.True(existing.IsSeller);
        Assert.False(existing.IsSellerVerified);
    }

    [Fact]
    public async Task RegisterAsync_HandlesNullFields()
    {
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync((UserProfile?)null);

        var request = new KunstButikken.UserService.Domain.Dtos.RegistrationRequest
        {
            Email = null,
            FullName = null,
            DisplayName = null,
            UserType = null
        };

        var result = await _service.RegisterAsync(userId, request);

        Assert.Equal(string.Empty, result.Email);
        Assert.Equal(string.Empty, result.FullName);
        Assert.Equal(string.Empty, result.DisplayName);
        Assert.False(result.IsSeller);
    }

    [Fact]
    public async Task GetOrCreateProfileAsync_NoClaims_UsesDefaults()
    {
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync((UserProfile?)null);

        // Principal with no claims and no identity name
        var principal = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity());

        var result = await _service.GetOrCreateProfileAsync(userId, principal);

        Assert.Equal("Anonymous", result.DisplayName);
        Assert.Equal(string.Empty, result.Email);
        Assert.Equal("Anonymous", result.FullName);
        Assert.False(result.IsSeller);
        Assert.False(result.IsAdmin);
        Assert.False(result.IsSellerVerified);
        _repo.Verify(r => r.AddAsync(It.Is<UserProfile>(p => p.UserId == userId), default), Times.Once);
    }

    private static ClaimsPrincipal BuildPrincipal(bool addRoles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "Test User"),
            new(ClaimTypes.Email, "test@example.com")
        };
        if (addRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, "seller"));
            claims.Add(new Claim(ClaimTypes.Role, "admin"));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }
}
