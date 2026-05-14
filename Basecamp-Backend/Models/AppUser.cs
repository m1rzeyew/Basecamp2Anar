using Microsoft.AspNetCore.Identity;

namespace Basecamp_Backend.Models
{
    public class AppUser:IdentityUser
    {
        public string FullName { get; set; }


        // Relationships
        public ICollection<ProjectMember> ProjectMembers { get; set; }
        public ICollection<Discussion> Discussions { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
