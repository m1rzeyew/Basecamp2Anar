namespace Basecamp_Backend.Models
{
    public class Project : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
        public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
        public ICollection<Discussion> Discussions { get; set; } = new List<Discussion>();
        public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
        public ICollection<ProjectThread> Threads { get; set; } = new List<ProjectThread>();
    }
}
