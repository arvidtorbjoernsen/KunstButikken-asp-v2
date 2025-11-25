namespace KunstButikken.UserService.Models;

public class UserProfile
{
  public Guid Id { get; set; }

  public Guid UserId { get; set; }

  // Keycloak user id (if user exists in Keycloak) - stored as string because Keycloak ids may not be GUIDs
  public string? KeycloakId { get; set; }
  public string DisplayName { get; set; } = string.Empty;
  public string Email { get; set; } = string.Empty;
  public string FullName { get; set; } = string.Empty;
  public bool IsSeller { get; set; }
  public bool IsSellerVerified { get; set; }
  public bool IsAdmin { get; set; }
  public string? ProfileImageUrl { get; set; }
  public string? PreferencesJson { get; set; }

  // Contact Information
  public string? PhoneNumber { get; set; }

  // Shipping Address
  public string? Address { get; set; }
  public string? City { get; set; }
  public string? PostalCode { get; set; }
  public string? Country { get; set; }

  public DateTimeOffset CreatedAt { get; set; }
  public DateTimeOffset? UpdatedAt { get; set; }
}