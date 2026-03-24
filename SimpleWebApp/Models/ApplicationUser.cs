using Microsoft.AspNetCore.Identity;

namespace SimpleWebApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
    }
}
