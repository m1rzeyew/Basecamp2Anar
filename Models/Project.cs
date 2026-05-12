using System.Net.Mail;

namespace Basecamp_Backend.Models
{
    public class Project:BaseEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }

        // Relationships
        public ICollection<ProjectMember> Members { get; set; }
        public ICollection<ProjectTask> Tasks { get; set; }
        public ICollection<Discussion> Discussions { get; set; }
        public ICollection<Attachment> Attachments { get; set; }
    }
}
