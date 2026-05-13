namespace Basecamp_Backend.Models
{
    public class ProjectMember : BaseEntity
    {
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;
        public string AppUserId { get; set; } = string.Empty;
        public AppUser AppUser { get; set; } = null!;
        public string Role { get; set; } = "Member";
    }
}
