using Dummy.SharedLib.Abstract;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace AuthAPI.Core.Domain.Entities
{
    /// <summary>
    /// Represents a user account.
    /// </summary>
    public class ApplicationUser : IdentityUser, IEntity<string>
    {
        public string Name { get; set; } = string.Empty;
    }
}
