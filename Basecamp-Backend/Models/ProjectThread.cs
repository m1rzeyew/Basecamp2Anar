namespace Basecamp_Backend.Models
{
    public class ProjectThread : BaseEntity
    {
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public string Title { get; set; } = string.Empty;

        public string? CreatedByUserId { get; set; }
        public AppUser? CreatedByUser { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ThreadMessage> Messages { get; set; } = new List<ThreadMessage>();
    }
}
