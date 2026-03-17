using Microsoft.AspNetCore.Identity;

namespace ZPassFit.Data.Models;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
