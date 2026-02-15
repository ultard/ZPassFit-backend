using Microsoft.AspNetCore.Identity;

namespace ZPassFit.Models;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
