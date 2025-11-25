namespace KunstButikken.UserService.Controllers;

public class RegistrationRequest
{
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? DisplayName { get; set; }
    public string? UserType { get; set; }
}

public class VerifyRequest
{
    public bool Verified { get; set; }
}

public class UnverifiedSellerDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
