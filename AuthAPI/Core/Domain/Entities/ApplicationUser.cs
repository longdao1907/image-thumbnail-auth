using Microsoft.AspNetCore.Identity;

namespace AuthAPI.Core.Domain.Entities
{
    /// <summary>
    /// Represents a user account.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
        }
}
