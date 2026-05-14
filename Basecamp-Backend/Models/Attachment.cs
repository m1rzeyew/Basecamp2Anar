namespace Basecamp_Backend.Models
{
    public class Attachment : BaseEntity
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;

        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public string? UploadedByUserId { get; set; }
        public AppUser? UploadedByUser { get; set; }
    }
}
