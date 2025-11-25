namespace KunstButikken.AdminService.Models;

public class AdminLog
{
  public Guid Id { get; set; }
  public DateTimeOffset CreatedAt { get; set; }
  public string Action { get; set; } = string.Empty;
  public string PerformedBy { get; set; } = "system";
  public string Details { get; set; } = string.Empty;
}