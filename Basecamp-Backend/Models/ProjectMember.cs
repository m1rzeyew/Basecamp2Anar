namespace Basecamp_Backend.Models
{
    public class ProjectMember:BaseEntity
    {
        public int ProjectId { get; set; }
        public Project Project { get; set; }

        public string AppUserId { get; set; }
        public AppUser AppUser { get; set; }

        // Layihə daxili rol: "Owner", "Admin", "Member"
        public string Role { get; set; }
    }
}
