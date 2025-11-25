namespace KunstButikken.AdminService.Models;

public class AdminActionResponse
{
    public Guid ArtId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

