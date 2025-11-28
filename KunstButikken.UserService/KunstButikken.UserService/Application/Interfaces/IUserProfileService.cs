namespace KunstButikken.UserService.Application.Interfaces;

using System.Security.Claims;
using KunstButikken.UserService.Domain.Dtos;
using KunstButikken.UserService.Domain.Entities;

public interface IUserProfileService
{
    Task<UserProfile> GetOrCreateProfileAsync(Guid userId, ClaimsPrincipal principal, CancellationToken ct = default);
    Task<UserProfile> UpdateProfileAsync(Guid userId, UserProfile input, CancellationToken ct = default);
    Task<UserProfile> RegisterAsync(Guid userId, RegistrationRequest request, CancellationToken ct = default);
}
