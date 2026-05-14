using Microsoft.AspNetCore.Identity;

namespace Basecamp_Backend.Models
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        public ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
        public ICollection<Discussion> Discussions { get; set; } = new List<Discussion>();
        public ICollection<Attachment> UploadedAttachments { get; set; } = new List<Attachment>();
        public ICollection<ProjectThread> CreatedThreads { get; set; } = new List<ProjectThread>();
        public ICollection<ThreadMessage> ThreadMessages { get; set; } = new List<ThreadMessage>();

        public bool IsDeleted { get; set; } = false;
    }
}
