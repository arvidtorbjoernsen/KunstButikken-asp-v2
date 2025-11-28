namespace KunstButikken.UserService.Domain.Dtos;

public sealed class RegistrationRequest
{
    public string? Email { get; init; }
    public string? FullName { get; init; }
    public string? DisplayName { get; init; }
    public string? UserType { get; init; }
}

public sealed class VerifyRequest
{
    public bool Verified { get; init; }
}

public sealed class UnverifiedSellerDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

public sealed class SellerDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
