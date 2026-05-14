namespace Basecamp_Backend.Models
{
    public class Discussion : BaseEntity
    {
        public string Content { get; set; }

        public string AppUserId { get; set; }
        public AppUser AppUser { get; set; }

        public int ProjectId { get; set; }
        public Project Project { get; set; }
    }
}
