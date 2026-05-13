namespace Basecamp_Backend.Models
{
    public class Discussion : BaseEntity
    {
        public string Content { get; set; } = string.Empty;
        public string AppUserId { get; set; } = string.Empty;
        public AppUser AppUser { get; set; } = null!;
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;
    }
}
