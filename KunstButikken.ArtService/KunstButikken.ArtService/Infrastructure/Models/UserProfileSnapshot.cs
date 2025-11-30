namespace KunstButikken.ArtService.Infrastructure.Models;

internal sealed class UserProfileSnapshot
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsSeller { get; set; }
    public bool IsSellerVerified { get; set; }
    public bool IsAdmin { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
