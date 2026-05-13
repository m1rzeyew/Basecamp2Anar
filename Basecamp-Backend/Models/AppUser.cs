using Microsoft.AspNetCore.Identity;

namespace Basecamp_Backend.Models
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
        public ICollection<Discussion> Discussions { get; set; } = new List<Discussion>();
        public bool IsDeleted { get; set; } = false;
    }
}
