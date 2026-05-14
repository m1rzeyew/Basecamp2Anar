namespace Basecamp_Backend.Models
{
    public class ThreadMessage : BaseEntity
    {
        public int ProjectThreadId { get; set; }
        public ProjectThread ProjectThread { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;

        public string Content { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
