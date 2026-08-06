namespace FantasyLeague.Domain.Entities.Users;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Username { get; set; }

    public required string Email { get; set; }

    public required string Password { get; set; }

    public string TimeZoneId { get; set; } = "UTC";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public UserRole Role { get; set; } = UserRole.User;

}
