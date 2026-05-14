namespace Basecamp_Backend.Models
{
    public class ProjectTask : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }

        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;
    }
}
